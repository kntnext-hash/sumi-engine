$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Color].Assembly.Location,'System.Runtime',(Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),(Join-Path $PSHOME 'System.Private.Windows.Core.dll')) -TypeDefinition @'
using System;using System.Drawing;
public static class RingScan {
public static double[] Scan(string path){using(var b=new Bitmap(path)){
long z=0,p=0,o=0,inner25=0,inner=0,outer=0,band=0,visible=0;int x0=b.Width,y0=b.Height,x1=-1,y1=-1;double minR=9999,maxR=0;
for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var a=b.GetPixel(x,y).A;double dx=x-(b.Width-1)/2.0,dy=y-(b.Height-1)/2.0,r=Math.Sqrt(dx*dx+dy*dy)/b.Width;
if(a==0)z++;else {if(a==255)o++;else p++;x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);if(r<.25)inner25++;if(r<.36)inner++;if(x<b.Width*.05||x>=b.Width*.95||y<b.Height*.05||y>=b.Height*.95)outer++;}
if(a>=32){visible++;if(r>=.38&&r<=.45)band++;minR=Math.Min(minR,r);maxR=Math.Max(maxR,r);}}
return new double[]{b.Width,b.Height,z,p,o,x0,y0,x1,y1,inner,outer,visible,band,minR,maxR,inner25};}}
}
'@
$f=Get-Item (Join-Path $PSScriptRoot 'core_enso_09.png');$v=[RingScan]::Scan($f.FullName)
[pscustomobject]@{file=$f.Name;width=$v[0];height=$v[1];bytes=$f.Length;sha256=(Get-FileHash $f.FullName -Algorithm SHA256).Hash;alphaZero=$v[2];alphaPartial=$v[3];alphaOpaque=$v[4];bounds=@($v[5],$v[6],$v[7],$v[8]);innerDiskRadius36PercentNonzeroAlpha=$v[9];outer5PercentNonzeroAlpha=$v[10];alphaAtLeast32Pixels=$v[11];inRadius38To45Percent=$v[12];visibleMinRadius=$v[13];visibleMaxRadius=$v[14];innerDiskRadius25PercentNonzeroAlpha=$v[15]} | ConvertTo-Json | Tee-Object -FilePath (Join-Path $PSScriptRoot 'inspection.json')

