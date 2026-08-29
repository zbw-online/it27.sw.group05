Feature: Order management
    As office staff
    I want to create and maintain orders for existing customers
    So that we can record what a customer has bought and for how much

Background:
    Given a customer "CU30001" named "Jane Doe" is already registered
    And the article group "Furniture" exists
    And article "ART-40001" named "Oak Table" already exists in group "Furniture" priced at 120.00 CHF
    And article "ART-40002" named "Oak Chair" already exists in group "Furniture" priced at 45.00 CHF

Scenario: Creating an order for an existing customer with one valid article
    When I create order "ORD-2026-001" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    Then order "ORD-2026-001" is created successfully
    And order "ORD-2026-001" has 1 order line
    And the total for order "ORD-2026-001" is 120.00 CHF

Scenario: An order can contain multiple lines
    When I create order "ORD-2026-002" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
      | ART-40002      | 2        |
    Then order "ORD-2026-002" is created successfully
    And order "ORD-2026-002" has 2 order lines
    And the total for order "ORD-2026-002" is 210.00 CHF

Scenario: Quantity must be greater than zero
    When I create order "ORD-2026-003" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 0        |
    Then the order creation is rejected because the quantity must be positive

Scenario: An order cannot reference a nonexistent customer
    When I create order "ORD-2026-004" for an unknown customer with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    Then the order creation is rejected because the customer was not found

Scenario: An order line cannot reference a nonexistent article
    When I create order "ORD-2026-005" for customer "CU30001" with an unknown article and quantity 1
    Then the order creation is rejected because the article was not found

Scenario: Duplicate order numbers are rejected
    Given order "ORD-2026-006" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    When I create order "ORD-2026-006" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40002      | 1        |
    Then the order creation is rejected because the order number already exists

Scenario: The order total equals the sum of line quantity multiplied by snapshotted unit price
    When I create order "ORD-2026-007" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 2        |
      | ART-40002      | 3        |
    Then the total for order "ORD-2026-007" is 375.00 CHF

Scenario: Editing order lines recalculates the total
    Given order "ORD-2026-008" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    When I change the quantity of article "ART-40001" on order "ORD-2026-008" to 3
    Then the total for order "ORD-2026-008" is 360.00 CHF

Scenario: An order can be found by its order number
    Given order "ORD-2026-009" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    When I search orders for "ORD-2026-009"
    Then the order search returns exactly order "ORD-2026-009"

Scenario: Orders can be searched or listed
    Given order "ORD-2026-010" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    And order "ORD-2026-011" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40002      | 1        |
    When I list all orders
    Then the order list contains "ORD-2026-010" and "ORD-2026-011"

Scenario: Deleting an order also removes its order lines
    Given order "ORD-2026-012" already exists for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
    When I delete order "ORD-2026-012"
    Then order "ORD-2026-012" can no longer be found

Scenario: A failed operation does not leave partially persisted data
    When I create order "ORD-2026-013" for customer "CU30001" with lines:
      | ArticleNumber | Quantity |
      | ART-40001      | 1        |
      | ART-40002      | 0        |
    Then the order creation is rejected because the quantity must be positive
    And order "ORD-2026-013" can not be found by search
