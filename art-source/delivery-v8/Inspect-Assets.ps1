$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Color].Assembly.Location,'System.Runtime',(Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),(Join-Path $PSHOME 'System.Private.Windows.Core.dll')) -TypeDefinition @'
using System;
using System.Drawing;
public static class ArtAlphaScan {
 public static long[] Scan(string path) {
  using(var b=new Bitmap(path)) {
   long red=0,interior=0,z=0,p=0,o=0,x0=b.Width,y0=b.Height,x1=-1,y1=-1;
   for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) {
    var c=b.GetPixel(x,y);var a=c.A;if(a>=32 && c.R>100 && c.R>c.G*1.5 && c.R>c.B*1.5)red++; if(x>=300 && x<1872 && y>=240 && y<484 && a>0) interior++;
    if(a==0)z++;else if(a==255)o++;else p++;
    if(a>0){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
   }
   return new long[]{b.Width,b.Height,z,p,o,x0,y0,x1,y1,b.GetPixel(b.Width/2,b.Height/2).A,red};
  }
 }
}
'@
$root=$PSScriptRoot
$report=foreach($file in Get-ChildItem "$root/upgrade_*.png") {
 $v=[ArtAlphaScan]::Scan($file.FullName)
 $bmp=[System.Drawing.Bitmap]::new($file.FullName)
 $format=$bmp.PixelFormat.ToString(); $bmp.Dispose()
 [pscustomobject]@{file=$file.Name;width=$v[0];height=$v[1];format=$format;bytes=$file.Length;alphaZero=$v[2];alphaPartial=$v[3];alphaOpaque=$v[4];bounds=@($v[5],$v[6],$v[7],$v[8]);centerAlpha=$v[9];redDominantPixelsAlphaAtLeast32=$v[10];sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash}
}
$report | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot 'inspection-final.json')
$report | ConvertTo-Json -Depth 5




