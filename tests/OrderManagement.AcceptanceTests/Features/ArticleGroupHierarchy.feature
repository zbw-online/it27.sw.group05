Feature: Article group hierarchy
    As a catalog manager
    I want to see the full parent/child structure of article groups
    So that I can understand and navigate how the catalog is organised

Scenario: Viewing the full hierarchy from a top-level group
    Given the article group "Electronics" exists
    And the article group "Computers" exists under "Electronics"
    And the article group "Laptops" exists under "Computers"
    When I view the article group hierarchy starting at "Electronics"
    Then the hierarchy contains "Electronics", "Computers" and "Laptops" in that parent order
    And "Laptops" is 2 levels below "Electronics" in the hierarchy

Scenario: Viewing the hierarchy for the whole catalog
    Given the article group "Electronics" exists
    And the article group "Furniture" exists
    When I view the full article group hierarchy
    Then the hierarchy contains both "Electronics" and "Furniture" as top-level groups
