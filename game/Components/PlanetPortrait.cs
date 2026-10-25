using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class PlanetPortrait:VBoxContainer
{
    private readonly TextureRect _image=new(){Name="PlanetImage",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(0,170),SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ExpandFill};
    private readonly Label _neutral=DevelopmentUi.Text("Not surveyed");private PlanetStyleCatalog? _styles;private PlanetPortraitCache? _cache;private KnownPlanetView? _planet;
    public Vector2I RenderSize{get;set;}=new(192,192);
    public override void _Ready(){AddChild(_image);AddChild(_neutral);ShowPlanet(_planet);}
    public void Configure(PlanetStyleCatalog styles,PlanetPortraitCache cache){_styles=styles;_cache=cache;if(IsNodeReady())ShowPlanet(_planet);}
    public void ShowPlanet(KnownPlanetView? planet){_planet=planet;if(planet?.Appearance is not {} descriptor||_styles is null||_cache is null){_image.Texture=null;_image.Visible=false;_neutral.Visible=true;_neutral.Text=planet is null?"Not surveyed":"No saved planet illustration";return;}_neutral.Visible=false;_image.Visible=true;_image.Texture=_cache.Get(descriptor,_styles.Resolve(planet.PlanetTypeId,planet.EnvironmentId,descriptor.Variant),RenderSize);}
    public override void _ExitTree(){_image.Texture=null;}
}
