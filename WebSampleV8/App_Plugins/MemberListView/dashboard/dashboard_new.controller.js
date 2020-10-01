function memberListViewController($scope, $routeParams, $location, contentTypeResource, userService) {

    var currentUser = {};

    userService.getCurrentUser().then(function (user) {

        currentUser = user;

        //get the system media listview
        contentTypeResource.getPropertyTypeScaffold(-96)
            .then(function (dt) {

                $scope.fakeProperty = {
                    alias: "members",
                    config: dt.config,
                    description: "",
                    editor: dt.editor,
                    hideLabel: true,
                    id: 1,
                    label: "Members:",
                    validation: {
                        mandatory: false,
                        pattern: null
                    },
                    value: "",
                    view: dt.view
                };

                // tell the list view to list content at root
                $routeParams.id = -1;
            });
    })

}

angular.module("umbraco").controller("MemberListView.Dashboard.MemberListViewDashboardController", memberListViewController);