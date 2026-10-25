using System;
using Godot;
using OpenAntares.Simulation.Appearance;
namespace OpenAntares.Game.Presentation;
/// <summary>Original cosmetic spherical surfaces. No simulation random source or world reference.</summary>
public static class PlanetPortraitRenderer
{
    public static Image Render(PlanetAppearanceDescriptor descriptor,PlanetStyle style,Vector2I size)
    {
        if(descriptor is not {Version:1,Variant:>=0 and <8}||size.X is <16 or >512||size.Y is <16 or >512||style.SurfacePalette.Length<2)throw new ArgumentException("Unsupported portrait descriptor, dimensions or palette.");
        var image=Image.CreateEmpty(size.X,size.Y,false,Image.Format.Rgba8);image.Fill(Colors.Transparent);
        ulong seed=descriptor.SurfaceSeed;float ox=(seed&65535)/2048f,oy=((seed>>16)&65535)/2048f,oz=((seed>>32)&65535)/2048f;
        for(int y=0;y<size.Y;y++)for(int x=0;x<size.X;x++) {
            float nx=(2f*x+1)/size.X-1,ny=(2f*y+1)/size.Y-1;float radius=nx*nx+ny*ny;if(radius>=0.96f)continue;float z=Mathf.Sqrt(1-radius);
            float u=Mathf.Atan2(nx,z),v=Mathf.Asin(ny);float coarse=Noise(nx*2.8f+ox,ny*2.8f+oy,z*2.8f+oz,seed),fine=Noise(nx*11+ox,ny*11+oy,z*11+oz,seed);
            float phase=(seed&1023)/163f;
            float height=style.PatternId switch {
                "continents"=>0.7f*coarse+0.3f*fine,
                "archipelago"=>0.48f*Noise(nx*8+ox,ny*8+oy,z*8+oz,seed)+0.3f*fine+0.22f*(0.5f+0.5f*Mathf.Sin(u*13+v*7+phase)),
                "riftlands"=>0.5f*coarse+0.15f*fine+0.35f*(0.5f+0.5f*Mathf.Sin(u*6+v*4+phase)),
                "crescent"=>0.45f*coarse+0.15f*fine+0.4f*(0.5f+0.5f*Mathf.Cos(u*2.6f+v+phase)),
                "polar"=>0.45f*coarse+0.2f*fine+0.35f*Mathf.Abs(ny),
                "shattered"=>0.35f*coarse+0.25f*fine+0.4f*Mathf.Abs(Mathf.Sin(u*9+v*11+phase)),
                "great_basin"=>0.45f*coarse+0.15f*fine+0.4f*(0.5f+0.5f*Mathf.Cos(u*2-v*2+phase)),
                "striated"=>0.3f*coarse+0.2f*fine+0.5f*(0.5f+0.5f*Mathf.Sin(v*14+u*4+phase)),
                _=>throw new ArgumentException("Unknown surface structure.")};
            height=Mathf.Clamp(height,0,1);Color color;
            if(style.OceanThreshold>0&&height<style.OceanThreshold)color=style.OceanColor.Lerp(style.SurfacePalette[0],Mathf.Clamp(height/style.OceanThreshold,0,1)*0.18f);
            else {float land=style.OceanThreshold==0?height:(height-style.OceanThreshold)/(1-style.OceanThreshold);float palette=Mathf.Clamp(land,0,0.9999f)*(style.SurfacePalette.Length-1);int index=(int)palette;color=style.SurfacePalette[index].Lerp(style.SurfacePalette[index+1],palette-index);}
            float clouds=Noise(nx*6+ox+13,ny*9+oy,z*6+oz,seed^0xa71cUL);float cloud=Mathf.SmoothStep(1-style.CloudCoverage,1,clouds);color=color.Lerp(style.CloudColor,cloud*0.65f);
            float light=0.2f+0.8f*Mathf.Max(0,-0.4f*nx-0.3f*ny+0.86f*z);color.R*=light;color.G*=light;color.B*=light;color.A=1;image.SetPixel(x,y,color);
        }
        return image;
    }
    private static float Noise(float x,float y,float z,ulong seed)
    {
        int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y),iz=Mathf.FloorToInt(z);float fx=Smooth(x-ix),fy=Smooth(y-iy),fz=Smooth(z-iz);
        float a=Mathf.Lerp(Hash(ix,iy,iz,seed),Hash(ix+1,iy,iz,seed),fx),b=Mathf.Lerp(Hash(ix,iy+1,iz,seed),Hash(ix+1,iy+1,iz,seed),fx),c=Mathf.Lerp(Hash(ix,iy,iz+1,seed),Hash(ix+1,iy,iz+1,seed),fx),d=Mathf.Lerp(Hash(ix,iy+1,iz+1,seed),Hash(ix+1,iy+1,iz+1,seed),fx);
        return Mathf.Lerp(Mathf.Lerp(a,b,fy),Mathf.Lerp(c,d,fy),fz);
    }
    private static float Smooth(float value)=>value*value*(3-2*value);
    private static float Hash(int x,int y,int z,ulong seed){ulong h=unchecked(seed^(ulong)(long)x*0x9e3779b97f4a7c15UL^(ulong)(long)y*0xbf58476d1ce4e5b9UL^(ulong)(long)z*0x94d049bb133111ebUL);h=unchecked((h^(h>>30))*0xbf58476d1ce4e5b9UL);h=unchecked((h^(h>>27))*0x94d049bb133111ebUL);return ((h^(h>>31))&0xffffff)/16777215f;}
}
