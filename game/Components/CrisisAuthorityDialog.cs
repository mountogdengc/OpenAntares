using System;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
namespace OpenAntares.Game.Components;
public partial class CrisisAuthorityDialog:AcceptDialog
{
    private CrisisView? _view;private readonly OptionButton _authority=new(){Name="CrisisAuthorityPicker"},_share=new(){Name="CrisisAuthorityShare"};
    public event Action<Command>? CommandRequested;
    public override void _Ready(){Title="Crisis authority";Name="CrisisAuthority";OkButtonText="Cancel";var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Crisis authority is separate from colony workforce and construction governors. No automatic policy can purge a colony. Manual responses pause only their crisis channel; Resume is explicit."));body.AddChild(_authority);foreach(string label in new[]{"Manual","Contain","Contain and research"})_authority.AddItem(label);body.AddChild(_share);foreach(int share in new[]{0,25,50,100}){_share.AddItem(share+"% new research");_share.SetItemMetadata(_share.ItemCount-1,share);}DevelopmentUi.Button(body,"Apply crisis authority",()=>{if(_view is {} v){Hide();CommandRequested?.Invoke(new SetCrisisAuthorityCommand(v.Viewer,v.Turn,(CrisisAuthority)_authority.Selected,_share.GetSelectedMetadata().AsInt32()));}},"CrisisApplyAuthority");}
    public void ShowAuthority(CrisisView view){_view=view;_authority.Select((int)view.Authority);for(int i=0;i<_share.ItemCount;i++)if(_share.GetItemMetadata(i).AsInt32()==view.AuthorityResearchShare)_share.Select(i);PopupCenteredClamped(new(620,420));}
}
