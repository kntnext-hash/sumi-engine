$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Color].Assembly.Location,'System.Runtime',(Join-Path $PSHOME 'System.Private.Windows.GdiPlus.dll'),(Join-Path $PSHOME 'System.Private.Windows.Core.dll')) -TypeDefinition @'
using System;
using System.Drawing;
public static class DepthScan {
 public static double[] Scan(string path) {
  using(var b=new Bitmap(path)) {
   long colored=0,alpha=0,n=0,dark=0;double sum=0;int max=0,min=255;
   for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
    var c=b.GetPixel(x,y);int d=Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B));
    if(d>0)colored++;max=Math.Max(max,d);if(c.A!=255)alpha++;
    if(x>=b.Width/4&&x<b.Width*3/4&&y>=b.Height/2){int v=(c.R+c.G+c.B)/3;sum+=v;n++;min=Math.Min(min,v);if(v<160)dark++;}
   }
   return new double[]{b.Width,b.Height,colored,max,alpha,n,sum/n,min,dark};
  }
 }
}
'@
$file=Get-Item (Join-Path $PSScriptRoot 'landscape_depth_05.png')
$v=[DepthScan]::Scan($file.FullName)
$b=[System.Drawing.Bitmap]::new($file.FullName);$format=$b.PixelFormat.ToString();$b.Dispose()
[pscustomobject]@{file=$file.Name;width=$v[0];height=$v[1];decodedFormat=$format;bytes=$file.Length;sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash;nonGrayPixels=$v[2];maxRgbChannelDifference=$v[3];nonOpaquePixels=$v[4];centralLowerRegion='x25%-75%,y50%-100%';regionPixelCount=$v[5];regionMeanRgb8bit=$v[6];regionMinRgb8bit=$v[7];regionPixelsBelow160=$v[8]} | ConvertTo-Json | Tee-Object -FilePath (Join-Path $PSScriptRoot 'inspection.json')
