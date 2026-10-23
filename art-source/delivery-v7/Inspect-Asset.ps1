$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Color].Assembly.Location,'System.Runtime',(Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),(Join-Path $PSHOME 'System.Private.Windows.Core.dll')) -TypeDefinition @'
using System;
using System.Drawing;
public static class ArtAlphaScan {
 public static long[] Scan(string path) {
  using(var b=new Bitmap(path)) {
   long minA=255,maxA=0,maxRGB=0,interior=0,z=0,p=0,o=0,x0=b.Width,y0=b.Height,x1=-1,y1=-1;
   for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) {
    var color=b.GetPixel(x,y); var a=color.A; if(x>=180 && x<844 && y>=220 && y<1316){minA=Math.Min(minA,a);maxA=Math.Max(maxA,a);maxRGB=Math.Max(maxRGB,Math.Max(color.R,Math.Max(color.G,color.B)));} if(x>=180 && x<844 && y>=220 && y<1316 && a>0) interior++;
    if(a==0)z++;else if(a==255)o++;else p++;
    if(a>0){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
   }
   return new long[]{b.Width,b.Height,z,p,o,x0,y0,x1,y1,b.GetPixel(b.Width/2,b.Height/2).A,interior,minA,maxA,maxRGB};
  }
 }
}
'@
$root=$PSScriptRoot
$report=foreach($file in Get-ChildItem "$root/washi_card_07.png") {
 $v=[ArtAlphaScan]::Scan($file.FullName)
 $bmp=[System.Drawing.Bitmap]::new($file.FullName)
 $format=$bmp.PixelFormat.ToString(); $bmp.Dispose()
 [pscustomobject]@{file=$file.Name;width=$v[0];height=$v[1];format=$format;bytes=$file.Length;alphaZero=$v[2];alphaPartial=$v[3];alphaOpaque=$v[4];bounds=@($v[5],$v[6],$v[7],$v[8]);centerAlpha=$v[9];sliceCenterNonzeroAlpha=$v[10];sliceCenterMinAlpha=$v[11];sliceCenterMaxAlpha=$v[12];sliceCenterMaxRgbChannel=$v[13];sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash}
}
$report | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot 'inspection-final.json')
$report | ConvertTo-Json -Depth 5





