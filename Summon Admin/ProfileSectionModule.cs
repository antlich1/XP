using Client;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MaterialDesignSkin.Modules
{
    //add module title and description, islocalized=false disables localization of the strings
    [ModuleDescription("Profile", "Manage your profile", IsLocalized = false)]
    //this will set module icon based on material design icons
    [ModuleIcon("USER")]
    //this will set the display order of module
    [ModuleDisplayOrder(3)]
    [ModuleType(typeof(ProfileSectionModule))]
    //any unique guid
    [ModuleGuid("2538622C-0F94-439D-99B6-5DECEFCBEE27")]
    //export module for dependency injection
    [Export(typeof(IClientSectionModule))]
    public class ProfileSectionModule : ClientSectionModuleBase
    {
        public override async Task SwitchInAsync(CancellationToken ct)
        {
            await base.SwitchInAsync(ct);

            //the module is being switched in, launch your browser here 

            //here you can get current user identity
            var currentUserIdentity = Client.CurrentUserIdentity;

            //check if identity set, otherwise no user is logged in
            if (currentUserIdentity == null)
                return;

            //get current user id
            var userId = currentUserIdentity.UserId;

            //launch your browser here

            Process.Start($"http://localhost/user/{userId}");

        }
    }
}
