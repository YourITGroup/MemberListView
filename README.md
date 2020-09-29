# umbMemberListView

![Member List View Logo](https://raw.githubusercontent.com/YourITGroup/umbMemberListView/master/assets/Membership_logo.png)

Nuget Package: 
[![NuGet release](https://img.shields.io/nuget/v/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)
[![NuGet release](https://img.shields.io/nuget/dt/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)

Umbraco Package:
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-orange.svg)](https://our.umbraco.org/projects/backoffice-extensions/memberlistview-for-umbraco-7) 

Adds a MemberListView dashboard to the Members area in Umbraco 7.7+ to allow easier management of members including approval and unlocking capabilities.
The MemberListView for Umbraco 7 provides a management dashboard view for Members with convenient filtering and sorting and allows for mass Unlock, Suspension or Activation of members.

## Features

The MemberListView has been designed to be similar to the ContentListView property editor and features context-sensitive action buttons to Unlock, Approve or Suspend members depending on the status of their account as well as the ability to Delete them. All actions can be performed on batches of users, as they apply to selected users only.

* Supports Umbraco Sensitive Data and GDPR
* Export members in either CSV or Excel OpenDocument format with the flexibility to select some or of the properties
* Enhanced filtering including filtering on Member Type and Group out of the box.
* Bulk member actions including Suspension, Unlock, Approve and Delete.

## Support Options

For bugs and general feature requests please use the Issue tracker here on Github at https://github.com/YourITGroup/umbMemberListView/issues

For help with installation and configuration, we recommend the Our.Umbraco forum here: https://our.umbraco.com/packages/backoffice-extensions/memberlistview-for-umbraco-7/say-hello/

For help with customisation and integration with third party packages not covered above, Your IT Team offer a paid-for service.  To start a discussion and find out more, visit https://youritteam.com.au/products/open-source#memberlistview 


## Version History

**Version 1.5.4**

* Improved handling of bulk operations - refresh should now display correct items after a short delay to allow the index to catch up.

**Version 1.5.3**

* Added support for SensitiveData - Exports will be hidden if the user doesn't have permission to view sensitive data, and sensitive properties will not be viewable
* Enhanced Export functionality - now allows selection of columns and the choice of exporting to CSV or Excel OpenFormat

**Version 1.5.0**

* **Compiled against Umbraco 7.15.1**
* Refreshed to be more inline with current Umbraco styling and practices.
* Filtering and columns now much easier to customise.
* Added multiple Member Group filtering out of the box

**Version 1.3.0**
* Support for Localization with plugin-based lang files
* Fixed issue where selecting a row caused the "You have unsaved changes" message to appear on navigation.

**Version 1.2.0**

* Compiled against Umbraco 7.7
* Fixed issue with suspending/activating/deleting members

**Version 0.9.12**

* Fixed "Error: Argument 'MemberManager.Dashboard.MemberListViewController' is not a function" issue due to update in ClientDependency module

**Version 0.9.11**

* UI cleanup - paging now consistent with 7.2.0 ListView
* Issue with UnApproved members showing up in the list as approved.

**Version 0.9.10**

* Export - Export now supports additional user indexed fields. If you add a property to a member type and want it to be exported, add it to the IndexUserFields collection in the ExamineIndex.config file.

**Version 0.9.9**

* Search is now lightning fast, although some modification to the Examine Index configuration is required. 
* Filtering - filtering has now been moved to a dialog, includes Membership Flags and MemberType as default fields and is extendable.
* Export - Filtered members can now be exported to a CSV file containing basic fields. 

## Sample Web project:

  * Uses SqlCe database - username is "admin@admin"; password is "password"
  * Upgraded to Umbraco 7.12.0
  
The Sample project has two member properties: `First Name` and `Last Name`.
The version of MemberListView in this project has been modified to include these two properties in the list and examine index
