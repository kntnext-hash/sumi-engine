using System;
using System.IO;
using System.Reflection;
namespace Bokumyaku.Simulation
{
    public sealed partial class RunSimulator
    {
        // JsonUtility can parse a round-trip double string to an adjacent IEEE754 value.
        // The payload is authoritative; public numeric fields remain inspectable and are
        // checked for consistency, never used to regenerate a payload during Restore.
        static string EncodeExactDoubles(RunSnapshot snapshot)
        {
            using(var stream=new MemoryStream())
            using(var writer=new BinaryWriter(stream))
            {
                writer.Write(1); // bit-payload version
                int count=0; VisitDoubles(snapshot,(owner,field)=>count++); writer.Write(count);
                VisitDoubles(snapshot,(owner,field)=>writer.Write((double)field.GetValue(owner)));
                writer.Flush(); return Convert.ToBase64String(stream.ToArray());
            }
        }
        static RunSnapshot DecodeExactDoubles(RunSnapshot source)
        {
            // Bound arrays and payload before cloning/traversing any untrusted input.
            Check(source.enemies!=null&&source.enemies.Length<=EnemyLimit&&source.branches!=null&&source.branches.Length<=64&&source.fields!=null&&source.fields.Length<=FieldLimit&&source.attacks!=null&&source.attacks.Length<=96,"snapshot array bounds");
            Check(source.levels!=null&&(source.levels.Length==6||source.levels.Length==7)&&source.spawnedByType!=null&&source.spawnedByType.Length==(source.rulesVersion>=8?7:5)&&source.killedByType!=null&&source.killedByType.Length==(source.rulesVersion>=8?7:5)&&source.leakedByType!=null&&source.leakedByType.Length==(source.rulesVersion>=8?7:5),"snapshot integer arrays");
            Check(!string.IsNullOrEmpty(source.exactDoubleBits)&&source.exactDoubleBits.Length<=65536,"exactDoubleBits missing/oversized");
            byte[] bytes;
            try { bytes=Convert.FromBase64String(source.exactDoubleBits); }
            catch(FormatException e) { throw new ArgumentException("Invalid exactDoubleBits encoding",e); }
            var copy=(RunSnapshot)CloneDto(source);
            int count=0; VisitDoubles(copy,(owner,field)=>count++);
            Check(bytes.Length==8+count*8,"exactDoubleBits length");
            using(var stream=new MemoryStream(bytes))
            using(var reader=new BinaryReader(stream))
            {
                Check(reader.ReadInt32()==1&&reader.ReadInt32()==count,"exactDoubleBits header");
                VisitDoubles(copy,(owner,field)=>
                {
                    double numeric=(double)field.GetValue(owner), exact=reader.ReadDouble();
                    Check(AdjacentOrEqual(numeric,exact),"numeric field differs from exactDoubleBits: "+field.Name);
                    field.SetValue(owner,exact);
                });
            }
            return copy;
        }
        static bool AdjacentOrEqual(double a,double b)
        {
            if(double.IsNaN(a)||double.IsInfinity(a)||double.IsNaN(b)||double.IsInfinity(b))return false;
            // Includes signed zero: preserve the payload sign when the numeric display loses it.
            if(a==b)return true;
            long x=BitConverter.DoubleToInt64Bits(a),y=BitConverter.DoubleToInt64Bits(b);
            if((x<0)!=(y<0))return false;
            // Same-sign finite doubles have a distance <= Int64.MaxValue; no Abs(MinValue).
            return x>y ? x-y==1 : y-x==1;
        }
        static FieldInfo[] OrderedFields(Type type)
        {
            var fields=type.GetFields(BindingFlags.Public|BindingFlags.Instance);
            Array.Sort(fields,(a,b)=>string.CompareOrdinal(a.Name,b.Name));return fields;
        }
        static void VisitDoubles(object value,Action<object,FieldInfo> action)
        {
            if(value==null)return;
            var type=value.GetType();
            if(type.IsPrimitive||type.IsEnum||value is string)return;
            if(value is Array){foreach(var item in (Array)value)VisitDoubles(item,action);return;}
            foreach(var field in OrderedFields(type))
                if(field.FieldType==typeof(double))action(value,field);
                else VisitDoubles(field.GetValue(value),action);
        }
        static object CloneDto(object value)
        {
            if(value==null)return null;
            var type=value.GetType();if(type.IsPrimitive||type.IsEnum||value is string)return value;
            if(value is Array)
            {
                var source=(Array)value;var copy=Array.CreateInstance(type.GetElementType(),source.Length);
                for(int i=0;i<source.Length;i++)copy.SetValue(CloneDto(source.GetValue(i)),i);return copy;
            }
            var result=Activator.CreateInstance(type);
            foreach(var field in OrderedFields(type))field.SetValue(result,CloneDto(field.GetValue(value)));
            return result;
        }
    }
}
