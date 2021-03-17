# Member List View for Umbraco 8

![Member List View Logo](https://raw.githubusercontent.com/YourITGroup/umbMemberListView/master/assets/Membership_logo.png)

Nuget Package: 
[![NuGet release](https://img.shields.io/nuget/v/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)
[![NuGet release](https://img.shields.io/nuget/dt/MemberListView.svg)](https://www.nuget.org/packages/MemberListView/)

Umbraco Package:
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-orange.svg)](https://our.umbraco.com/packages/backoffice-extensions/memberlistview/) 

Adds a MemberListView dashboard to the Members area in Umbraco 8.6+ to allow easier management of members including approval and unlocking capabilities.
The MemberListView for Umbraco 8 provides a management dashboard view for Members with convenient filtering and sorting and allows for mass Unlock, Suspension or Activation of members.

Installing the package enables a new Manage Dashboard on the Members section.

## Features

The Member List View has been designed to be similar to the ContentListView property editor and features a Create button to allow for quick creation of new Members and context-sensitive action buttons to Unlock, Approve or Suspend members depending on the status of their account as well as the ability to Delete them.  All actions can be performed on batches of users, as they apply to selected users only.

## Member Editing

Member Editing can be done without leaving the MemberListView by clicking on a member name, which creates a Dialog pulled in from the right side to edit the member.

## Version History

**Version 2.0.9**

* Fixes to work with Umbraco 8.12 - previously wasn't enforcing `"strict mode";` which was causing everything to fall in a heap.
* Aesthetic changes to checkboxes in dialogs

**Version 2.0.7**

* Various bug fixes including fix for Suspended/Locked filtering.

**Version 2.0.4**

* Backwards compatibility fix for Umbraco 8.6

**Version 2.0.0**

* Re-written for Umbraco 8 🎉

**Version 1.5.5**

* Shiny new icon - that's it 😎

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

  * Uses SqlCe database - username is "**admin@admin.com**"; password is "**Password123**"
  * Umbraco 8.7.0
  
The Sample project has three member properties: `First Name`, `Last Name` and `Phone Number`.  `Phone Number` has also been marked as Sensitive.
The version of MemberListView in the sample project has been modified to include these properties in the list.

## Logo
The package logo uses the Family (by Oksana Latysheva, UA) icon from the Noun Project, licensed under CC BY 3.0 US.
