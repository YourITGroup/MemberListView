# umbMemberListView

Nuget Package: 
[![NuGet release](https://img.shields.io/nuget/v/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)
[![NuGet release](https://img.shields.io/nuget/dt/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)

Umbraco Package:
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-orange.svg)](https://our.umbraco.org/projects/backoffice-extensions/memberlistview-for-umbraco-7) 

Adds a MemberListView dashboard to the Members area in Umbraco 7.7+ to allow easier management of members including approval and unlocking capabilities.
The MemberListView for Umbraco 7 provides a management dashboard view for Members with convenient filtering and sorting and allows for mass Unlock, Suspension or Activation of members.

Installing the package enables a new Manage Dashboard on the Members section.

## Features

The MemberListView has been designed to be similar to the ContentListView property editor and features a Create button to allow for quick creation of new Members and context-sensitive action buttons to Unlock, Approve or Suspend members depending on the status of their account as well as the ability to Delete them.  All actions can be performed on batches of users, as they apply to selected users only.

## Member Editing

Member Editing can be done without leaving the MemberListView by clicking on a member name, which creates a Dialog pulled in from the right side to edit the member.

## Version History

**Version 1.4.0**
* Support for Umbraco 7.15

**Version 1.3.0**
* Support for Localization with plugin-based lang files
* Fixed issue where selecting a row caused the "You have unsaved changes" message to appear on navigation.

**Version 1.2.0**

* **Compiled against Umbraco 7.7**
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

**Current issues**

Currently Bulk Deletion will throw a lot of errors due to Database commit blocking.  Deleting one at a time however should not be affected.


## Sample Web project:

  * Uses SqlCe database - username is "admin@admin"; password is "password"
  * Upgraded to Umbraco 7.12.0
  
The Sample project has two member properties: `First Name` and `Last Name`.
The version of MemberListView in this project has been modified to include these two properties in the list and examine index
