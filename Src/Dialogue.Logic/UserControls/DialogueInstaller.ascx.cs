﻿using System;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Web.UI;
using System.Xml;
using Dialogue.Logic.Constants;
using Dialogue.Logic.Models;
using Dialogue.Logic.Services;

namespace Dialogue.Logic.UserControls
{
    public partial class DialogueInstaller : UserControl
    {
        public InstallerResult InstallerResult { get; set; }
        private const string PFormat = "<p>{0}</p>";
        private const string HFormat = "<h5>{0}</h5>";

        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (Page.IsPostBack && InstallerResult != null && InstallerResult.ResultItems.Any())
            {
                // Show Results
                if (InstallerResult.CompletedSuccessfully())
                {
                    InstallerSuccessfull.Visible = true;
                    pnlMainText.Visible = false;
                }
                InstallerResultPanel.Visible = true;
                litResults.Text = FormatInstallerResults(InstallerResult);
            }
            else if (Page.IsPostBack)
            {
                InstallerResultPanel.Visible = true;
                litResults.Text = @"Installer results are empty? Try the installer again. 
                                    If the same thing happens, a manual check will be needed to make sure everything has installed correctly.";
            }
        }

        private string FormatInstallerResults(InstallerResult installerResult)
        {

            var sb = new StringBuilder();

            // Loop through results and display
            foreach (var resultItem in installerResult.ResultItems)
            {
                CreateResultEntry(resultItem, sb);
            }

            return sb.ToString();
        }

        private void CreateResultEntry(ResultItem resultItem, StringBuilder sb)
        {
            sb.Append("<div style='margin:10px 0;padding:10px 0;border-bottom:1px #efefef dotted'>").AppendLine();
            sb.AppendFormat(HFormat, resultItem.Name).AppendLine();
            sb.AppendFormat(PFormat, resultItem.Description).AppendLine();
            sb.AppendFormat(PFormat, string.Concat("Completed Successfully: ", resultItem.CompletedSuccessfully)).AppendLine();
            sb.Append("<div>").AppendLine();
        }

        protected void CompleteInstallation(object sender, EventArgs e)
        {
            // TODO - List of things todo
            // TODO - Must check if exists before doing, so it doesn't screw up on update

            InstallerResult = new InstallerResult();

            try
            {

                // Check member type exists
                var memberTypeResult = new ResultItem
                {
                    CompletedSuccessfully = false,
                    Name = string.Format("Creating the Member Type {0}", AppConstants.MemberTypeAlias)
                };
                var typeExists = ServiceFactory.MemberService.MemberTypeExists(AppConstants.MemberTypeAlias);
                if (!typeExists)
                {
                    // create the Dialogue membertype
                    ServiceFactory.MemberService.AddDialogueMemberType();
                    memberTypeResult.Description = "Done successfully";
                    memberTypeResult.CompletedSuccessfully = true;
                }
                else
                {
                    memberTypeResult.Description = "Skipped as member type already exists";
                    memberTypeResult.CompletedSuccessfully = true;
                }
                InstallerResult.ResultItems.Add(memberTypeResult);


                // Add Member Groups (Admin, Guest, Standard)
                var adminRoleResult = new ResultItem
                {
                    CompletedSuccessfully = false,
                    Name = string.Format("Creating the role: {0}", AppConstants.AdminRoleName)
                };
                var adminExists = ServiceFactory.MemberService.MemberGroupExists(AppConstants.AdminRoleName);
                if (!adminExists)
                {
                    // Create it
                    ServiceFactory.MemberService.CreateMemberGroup(AppConstants.AdminRoleName);
                    adminRoleResult.Description = "Done successfully";
                    adminRoleResult.CompletedSuccessfully = true;
                }
                else
                {
                    adminRoleResult.Description = "Skipped as role already exists";
                    adminRoleResult.CompletedSuccessfully = true;
                }
                InstallerResult.ResultItems.Add(adminRoleResult);


                var guestRoleResult = new ResultItem
                {
                    CompletedSuccessfully = false,
                    Name = string.Format("Creating the role: {0}", AppConstants.GuestRoleName)
                };
                var guestExists = ServiceFactory.MemberService.MemberGroupExists(AppConstants.GuestRoleName);
                if (!guestExists)
                {
                    // Create it
                    ServiceFactory.MemberService.CreateMemberGroup(AppConstants.GuestRoleName);
                    guestRoleResult.Description = "Done successfully";
                    guestRoleResult.CompletedSuccessfully = true;
                }
                else
                {
                    guestRoleResult.Description = "Skipped as role already exists";
                    guestRoleResult.CompletedSuccessfully = true;
                }
                InstallerResult.ResultItems.Add(guestRoleResult);


                var standardRoleResult = new ResultItem
                {
                    CompletedSuccessfully = false,
                    Name = string.Format("Creating the role: {0}", AppConstants.MemberGroupDefault)
                };
                var standardExists = ServiceFactory.MemberService.MemberGroupExists(AppConstants.MemberGroupDefault);
                if (!standardExists)
                {
                    // Create it
                    ServiceFactory.MemberService.CreateMemberGroup(AppConstants.MemberGroupDefault);
                    standardRoleResult.Description = "Done successfully";
                    standardRoleResult.CompletedSuccessfully = true;
                }
                else
                {
                    standardRoleResult.Description = "Skipped as role already exists";
                    standardRoleResult.CompletedSuccessfully = true;
                }
                InstallerResult.ResultItems.Add(standardRoleResult);

                // Web.Config Stuff
                var updateConfig = false;
                var webConfigPath = HostingEnvironment.MapPath("~/web.config");
                if (webConfigPath != null)
                {
                    var xDoc = new XmlDocument();
                    xDoc.Load(webConfigPath);

                    // Entity Framework Configuration Sections
                    var efResult = new ResultItem
                    {
                        CompletedSuccessfully = false,
                        Name = "Add Entity Framework Config Sections"
                    };
                    if (!IsEntityFrameworkAlreadyInstalled(xDoc))
                    {
                        // TODO - Last as it will recycle app pool
                        // Add Entity Framework Entries into Web.config
                        AddEntityFrameworkConfigSections(xDoc);
                        efResult.CompletedSuccessfully = true;
                        efResult.Description = "Successfully added the config sections to the web.config";

                        // Tell the installer to save the config
                        updateConfig = true;
                    }
                    else
                    {
                        efResult.CompletedSuccessfully = true;
                        efResult.Description = "Entity Framework already installed, so skipped.";
                    }
                    InstallerResult.ResultItems.Add(efResult);


                    //TODO Add other web.config changes here if needed


                    if (updateConfig)
                    {
                        // Finally save web.config
                        xDoc.Save(webConfigPath);
                    }

                }
                else
                {
                    var nowebConfig = new ResultItem
                    {
                        CompletedSuccessfully = false,
                        Name = "No Web.Config?",
                        Description = "Installer is unable to locate the web.config using 'HostingEnvironment.MapPath(\"~/web.config\")'? Weird hey."
                    };
                    InstallerResult.ResultItems.Add(nowebConfig);
                }
            }
            catch (Exception ex)
            {
                var memberTypeResult = new ResultItem
                {
                    CompletedSuccessfully = false,
                    Name = "There was an error trying to installer",
                    Description = string.Concat(ex.Message, "<br/><br/>", ex.InnerException.Message)
                };
                InstallerResult.ResultItems.Add(memberTypeResult);
            }

        }

        private static bool IsEntityFrameworkAlreadyInstalled(XmlDocument webconfig)
        {
            var entityFrameworkConfig = webconfig.SelectSingleNode("configuration/configSections/section[@name='entityFramework']");
            return entityFrameworkConfig != null;
        }

        private static void AddEntityFrameworkConfigSections(XmlDocument webconfig)
        {

                // get the configSections
                var configSections = webconfig.SelectSingleNode("configuration/configSections");

                // Create new section
                var newSection = webconfig.CreateNode(XmlNodeType.Element, "section", null);

                // Attributes
                var nameAttr = webconfig.CreateAttribute("name");
                nameAttr.Value = "entityFramework";
                var typeAttr = webconfig.CreateAttribute("type");
                typeAttr.Value = "System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                var requirePermissionAttr = webconfig.CreateAttribute("requirePermission");
                requirePermissionAttr.Value = "false";
                newSection.Attributes.Append(nameAttr);
                newSection.Attributes.Append(typeAttr);
                newSection.Attributes.Append(requirePermissionAttr);

                // Append it
                configSections.AppendChild(newSection);

                // get the configSections
                var mainConfig = webconfig.SelectSingleNode("configuration");

                // Create <entityFramework>
                var entityFramework = webconfig.CreateNode(XmlNodeType.Element, "entityFramework", null);

                // Create 
                var defaultConnectionFactory = webconfig.CreateNode(XmlNodeType.Element, "defaultConnectionFactory", null);
                var dcType = webconfig.CreateAttribute("type");
                dcType.Value = "System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework";
                defaultConnectionFactory.Attributes.Append(dcType);
                entityFramework.AppendChild(defaultConnectionFactory);

                // Create Providers
                var providers = webconfig.CreateNode(XmlNodeType.Element, "providers", null);

                // Create Provider element
                var provider = webconfig.CreateNode(XmlNodeType.Element, "provider", null);
                var provinvariantName = webconfig.CreateAttribute("invariantName");
                provinvariantName.Value = "System.Data.SqlClient";
                provider.Attributes.Append(provinvariantName);
                var provType = webconfig.CreateAttribute("type");
                provType.Value = "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer";
                provider.Attributes.Append(provType);

                // Append Provide to Providers
                providers.AppendChild(provider);

                // Append Providers 
                entityFramework.AppendChild(providers);

                // Append Providers 
                mainConfig.AppendChild(entityFramework);

                //<entityFramework>
                //    <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework" />
                //    <providers>
                //        <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
                //    </providers>
                //</entityFramework>
       
        }
    }
}