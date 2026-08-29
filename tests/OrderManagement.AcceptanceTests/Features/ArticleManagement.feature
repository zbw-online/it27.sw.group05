Feature: Article management
    As a catalog manager
    I want to maintain articles with correct pricing and stock
    So that the catalog always reflects what can actually be sold

Background:
    Given the article group "Electronics" exists

Scenario: Adding a new article to a group
    When I add article "ART-00001" named "Wireless Mouse" to group "Electronics" priced at 24.90 CHF with stock 50
    Then article "ART-00001" exists in group "Electronics" with stock 50

Scenario: Rejecting a duplicate article number
    Given article "ART-00002" named "Keyboard" already exists in group "Electronics"
    When I add article "ART-00002" named "Duplicate Keyboard" to group "Electronics" priced at 39.90 CHF with stock 10
    Then the article registration is rejected because the article number already exists

Scenario: Increasing stock after a delivery
    Given article "ART-00003" named "Monitor" already exists in group "Electronics" with stock 5
    When I adjust stock for article "ART-00003" by 20
    Then article "ART-00003" has stock 25

Scenario: Rejecting a stock reduction below zero
    Given article "ART-00004" named "Webcam" already exists in group "Electronics" with stock 3
    When I adjust stock for article "ART-00004" by -10
    Then the stock adjustment is rejected because it would go below zero
    And article "ART-00004" still has stock 3

Scenario: Removing an article that is no longer sold
    Given article "ART-00005" named "Old Cable" already exists in group "Electronics"
    When I delete article "ART-00005"
    Then article "ART-00005" can no longer be found
