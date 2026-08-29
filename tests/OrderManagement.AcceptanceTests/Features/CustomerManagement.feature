Feature: Customer management
    As office staff
    I want to register and maintain customers
    So that orders can always be linked to a known, correctly identified customer

Scenario: Registering a new customer with a valid address
    Given no customer is registered with the number "CU10001"
    When I register a customer "CU10001" named "Jane Doe" with email "jane.doe@example.com" and address "Main Street 1, 8000 Zurich, CH"
    Then the customer "CU10001" is registered successfully
    And the customer "CU10001" has the address "Main Street 1, 8000 Zurich, CH"

Scenario: Rejecting a duplicate customer number
    Given a customer "CU10002" named "John Smith" is already registered
    When I register a customer "CU10002" named "Someone Else" with email "someone.else@example.com" and address "Second Street 2, 9000 St. Gallen, CH"
    Then the registration is rejected because the customer number already exists

Scenario: Rejecting a duplicate email address
    Given a customer "CU10003" named "Alice Adams" with email "alice.adams@example.com" is already registered
    When I register a customer "CU10004" named "Bob Brown" with email "alice.adams@example.com" and address "Second Street 2, 9000 St. Gallen, CH"
    Then the registration is rejected because the email already exists

Scenario: Updating a customer's name and email
    Given a customer "CU10005" named "Old Name" with email "old@example.com" is already registered
    When I update customer "CU10005" to name "New Name" and email "new@example.com"
    Then the customer "CU10005" has the name "New Name" and email "new@example.com"

Scenario: Searching customers by last name
    Given a customer "CU10006" named "Meier" is already registered
    And a customer "CU10007" named "Muster" is already registered
    When I search customers for "Meier"
    Then the search returns exactly the customers named "Meier"

Scenario: Deleting an existing customer
    Given a customer "CU10008" named "Temporary" is already registered
    When I delete customer "CU10008"
    Then customer "CU10008" can no longer be found
