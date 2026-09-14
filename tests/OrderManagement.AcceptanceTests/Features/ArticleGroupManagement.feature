Feature: Article group management
    As a catalog manager
    I want to organise articles into named groups and subgroups
    So that the catalog stays browsable as it grows

Scenario: Creating a top-level article group
    When I create the top-level article group "Furniture"
    Then the article group "Furniture" exists with no parent

Scenario: Creating a subgroup under an existing group
    Given the article group "Furniture" exists
    When I create the article group "Chairs" under "Furniture"
    Then the article group "Chairs" exists with parent "Furniture"

Scenario: Renaming an article group
    Given the article group "Misc" exists
    When I rename article group "Misc" to "Accessories"
    Then the article group "Accessories" exists
    And the article group "Misc" no longer exists

Scenario: Preventing deletion of a group that still contains articles
    Given the article group "Kitchen" exists
    And article "ART-00010" named "Blender" already exists in group "Kitchen"
    When I delete article group "Kitchen"
    Then the deletion is rejected because the group still contains articles

Scenario: Preventing deletion of a group that still has child groups
    Given the article group "Outdoor" exists
    And the article group "Garden Tools" exists under "Outdoor"
    When I delete article group "Outdoor"
    Then the deletion is rejected because the group still has child groups

Scenario: Deleting an empty leaf group
    Given the article group "Temporary" exists
    When I delete article group "Temporary"
    Then the article group "Temporary" no longer exists
