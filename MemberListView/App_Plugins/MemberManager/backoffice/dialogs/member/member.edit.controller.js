/**
 * @ngdoc controller
 * @name MemberManager.Dialogs.Member.EditController
 * @function
 * 
 * @description
 * The controller for the member editor
 */
function MemberEditDialogController($scope, $routeParams, $q, $timeout, $window, appState, memberResource, entityResource, navigationService, notificationsService, angularHelper, serverValidationManager, contentEditingHelper, fileManager, formHelper, umbRequestHelper, umbModelMapper, $http) {
    //setup scope vars
    $scope.model = {};
    $scope.model.defaultButton = null;
    $scope.model.subButtons = [];
    $scope.model.nodeId = 0;
    $scope.loaded = false;

    var dialogOptions = $scope.$parent.dialogOptions;

    function performGet() {
        var deferred = $q.defer();
        if (angular.isObject(dialogOptions.entity)) {
            $scope.loaded = true;
            deferred.resolve(dialogOptions.entity);
        } else {
            if (dialogOptions.create) {
                //we are creating so get an empty member item
                memberResource.getScaffold(dialogOptions.contentType)
                    .then(function (data) {

                        $scope.loaded = true;
                        deferred.resolve(data);
                    });
            }
            else {
                if (dialogOptions.id && dialogOptions.id.toString().length < 9) {
                    entityResource.getById(dialogOptions.id, "Member").then(function (entity) {
                        memberResource.getByKey(entity.key).then(function (data) {

                            //in one particular special case, after we've created a new item we redirect back to the edit
                            // route but there might be server validation errors in the collection which we need to display
                            // after the redirect, so we will bind all subscriptions which will show the server validation errors
                            // if there are any and then clear them so the collection no longer persists them.
                            serverValidationManager.executeAndClearAllSubscriptions();
                            $scope.loaded = true;

                            deferred.resolve(data);
                        });
                    });
                }
                else {
                    //we are editing so get the content item from the server
                    memberResource.getByKey(dialogOptions.id)
                        .then(function (data) {

                            $scope.loaded = true;

                            //in one particular special case, after we've created a new item we redirect back to the edit
                            // route but there might be server validation errors in the collection which we need to display
                            // after the redirect, so we will bind all subscriptions which will show the server validation errors
                            // if there are any and then clear them so the collection no longer persists them.
                            serverValidationManager.executeAndClearAllSubscriptions();

                            deferred.resolve(data);
                        });
                }
            }
        }

        return deferred.promise;
    };

    function performSave(args) {
        var deferred = $q.defer();

        $scope.busy = true;

        if (formHelper.submitForm({ scope: $scope, statusMessage: args.statusMessage })) {

            $scope.busy = true;

            args.saveMethod($scope.model.entity, $routeParams.create, fileManager.getFiles())
                .then(function (data) {

                    formHelper.resetForm({ scope: $scope, notifications: data.notifications });

                    contentEditingHelper.handleSuccessfulSave({
                        scope: $scope,
                        savedContent: data,
                        rebindCallback: contentEditingHelper.reBindChangedProperties($scope.model.entity, data)
                    });

                    $scope.busy = false;
                    deferred.resolve(data);

                }, function (err) {

                    contentEditingHelper.handleSaveError({
                        redirectOnFailure: false,
                        err: err,
                        rebindCallback: contentEditingHelper.reBindChangedProperties($scope.model.entity, err.data)
                    });

                    $scope.busy = false;
                    deferred.reject(err);
                });
        } else {
            $scope.busy = false;
            deferred.reject();
        }

        return deferred.promise;
    };

    performGet().then(function (content) {
        $scope.model.entity = $scope.filterTabs(content, dialogOptions.tabFilter);
    });

    $scope.filterTabs = function (entity, blackList) {
        if (blackList) {
            _.each(entity.tabs, function (tab) {
                tab.hide = _.contains(blackList, tab.alias);
            });
        }

        return entity;
    };

    $scope.save = function () {
        performSave({ saveMethod: memberResource.save, statusMessage: "Saving..." })
            .then(function (content) {
                if (dialogOptions.closeOnSave) {
                    $scope.submit(content);
                }
            });
    };

}

angular.module("umbraco").controller("MemberManager.Dialogs.Member.EditController", MemberEditDialogController);
