using System;
using System.IO;
using Bokumyaku.Simulation;
using UnityEngine;

namespace Bokumyaku.Persistence
{
    public static class MetaSaveStore
    {
        [Serializable] sealed class Envelope
        {
            public int schema = 1;
            public string config = "prototype-3enemy-v1";
            public MetaProgress meta;
        }
        static bool Valid(MetaProgress m) => m != null && m.Crystals >= 0 &&
            m.Damage >= 0 && m.Damage <= 20 && m.Supply >= 0 && m.Supply <= 20 &&
            m.Growth >= 0 && m.Growth <= 20 && m.Harvest >= 0 && m.Harvest <= 20;
        static MetaProgress Read(string path)
        {
            var e = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if (e == null || e.schema != 1 || e.config != "prototype-3enemy-v1" || !Valid(e.meta))
                throw new InvalidDataException("Unsupported or invalid prototype save.");
            return e.meta;
        }
        public static MetaProgress Load(string path, out string warning)
        {
            warning = "";
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new MetaProgress();
            try { return Read(path); }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                warning = "Save unreadable; recovered backup.";
                try { return Read(path + ".bak"); }
                catch (Exception b) when (b is IOException || b is InvalidDataException || b is ArgumentException || b is UnauthorizedAccessException)
                { warning = "Save unreadable; original preserved, starting temporary progress."; return new MetaProgress(); }
            }
        }
        public static void Save(string path, MetaProgress meta)
        {
            if (!Valid(meta)) throw new InvalidDataException("Invalid progress.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(new Envelope { meta = meta }, true));
            Read(temp);
            if (File.Exists(path))
            {
                bool valid = true;
                try { Read(path); } catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException) { valid = false; }
                // Never replace a good backup with a corrupt current file.
                if (valid) File.Replace(temp, path, path + ".bak");
                else { File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N")); File.Replace(temp, path, null); }
            }
            else File.Move(temp, path);
        }
    }
}

