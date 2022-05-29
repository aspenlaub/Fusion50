@FolderUpdater
Feature: FolderUpdater
	In order to update binary compiler output only if necessary
	I want to use a dedicated folder updater component

Background:
	Given I have an empty source and an empty destination folder beneath the user's temp folder

Scenario: Destination folder is created when missing
	Given The destination folder does not exist
	When I update the destination folder
	Then the destination folder exists
	And no errors occurred during the folder update

Scenario: Empty file is updated once
	Given I place an empty file into the source folder
	When I update the destination folder
	Then the empty file is present in the destination folder
	And the last write times of the empty source and the empty destination files are identical 
	And no errors occurred during the folder update
    When I wait two seconds
	And I overwrite the empty file in the source folder
	And I update the destination folder
	Then the last write time of the empty destination file is unchanged
	And no errors occurred during the folder update

Scenario: 4 kilobyte file is updated
	Given I place a 4 kilobyte file into the source folder
    And I wait two seconds
	And I place a 4 kilobyte file with 30 differences into the destination folder
	When I update the destination folder
	Then the 4 kilobyte destination file is identical to the 4 kilobyte source file
	And the last write times of the 4 kilobyte source and the 4 kilobyte destination files are identical 
	And no errors occurred during the folder update

Scenario: 4 kilobyte file is not updated
	Given I place a 4 kilobyte file into the source folder
    And I wait two seconds
	And I place a 4 kilobyte file with 20 differences into the destination folder
	When I update the destination folder
	Then the last write time of the 4 kilobyte destination file is unchanged
	And no errors occurred during the folder update

Scenario: 3 kilobyte file is updated
	Given I place a 3 kilobyte file into the source folder
    And I wait two seconds
	And I place a 3 kilobyte file with 20 differences into the destination folder
	When I update the destination folder
	Then the 3 kilobyte destination file is identical to the 3 kilobyte source file
	And the last write times of the 3 kilobyte source and the 3 kilobyte destination files are identical 
	And no errors occurred during the folder update

Scenario: File in a sub folder is updated
	Given I place an empty file into a sub folder of the source folder
	When I update the destination folder
	Then the empty file is present in a sub folder of the destination folder
	And the last write times of the empty source and the empty destination files in the sub folders are identical 
	And no errors occurred during the folder update

Scenario: Locked 3 kilobyte file cannot be renamed
	Given I place a 3 kilobyte file into the source folder
    And I wait two seconds
	And I place a 3 kilobyte file with 20 differences into the destination folder
	And I lock the 3 kilobyte file
	When I update the destination folder
	Then folder update failed because the 3 kilobyte destination file could not be renamed