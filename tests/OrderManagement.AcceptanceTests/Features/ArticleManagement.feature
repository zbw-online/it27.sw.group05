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

Scenario: A referenced article cannot be permanently deleted
    Given article "ART-00006" named "Referenced Item" already exists in group "Electronics"
    And a customer "CU40001" named "Order Customer" is already registered
    And order "ORD-2026-030" already exists for customer "CU40001" with lines:
      | ArticleNumber | Quantity |
      | ART-00006      | 1        |
    When I delete article "ART-00006"
    Then the article deletion is rejected because it is referenced by an order

Scenario: A referenced article can be deactivated and disappears from the active catalogue
    Given article "ART-00007" named "Deactivatable Item" already exists in group "Electronics"
    And a customer "CU40002" named "Another Customer" is already registered
    And order "ORD-2026-031" already exists for customer "CU40002" with lines:
      | ArticleNumber | Quantity |
      | ART-00007      | 1        |
    When I deactivate article "ART-00007"
    Then article "ART-00007" is inactive
    And article "ART-00007" is excluded from the active article catalogue

Scenario: Filtering by a parent category includes articles from descendant groups
    Given the article group "Computers" exists under "Electronics"
    And article "ART-00008" named "Desktop PC" already exists in group "Electronics"
    And article "ART-00009" named "Laptop" already exists in group "Computers"
    When I filter articles by category "Electronics"
    Then the filtered article list contains "ART-00008" and "ART-00009"
