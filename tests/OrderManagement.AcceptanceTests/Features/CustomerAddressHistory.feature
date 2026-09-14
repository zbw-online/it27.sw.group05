Feature: Customer address history
    As office staff
    I want each customer's address changes to be kept as history
    So that we always know which address applied on a given date, including future moves

Scenario: Adding a future address closes the current one
    Given a customer "CU20001" is registered with address "Old Street 1, 8000 Zurich, CH" valid from "2026-01-01"
    When I add a future address "New Street 5, 9000 St. Gallen, CH" for customer "CU20001" valid from "2027-01-01"
    Then customer "CU20001" has 1 future address
    And the future address for customer "CU20001" is "New Street 5, 9000 St. Gallen, CH"

Scenario: Viewing previous, current and future addresses separately
    Given a customer "CU20002" is registered with address "First Street 1, 8000 Zurich, CH" valid from "2025-01-01"
    And customer "CU20002" moved to "Second Street 2, 8000 Zurich, CH" valid from "2026-01-01"
    And customer "CU20002" moved to "Third Street 3, 8000 Zurich, CH" valid from "2027-01-01"
    When I view the address history for customer "CU20002"
    Then the current address for customer "CU20002" is "Second Street 2, 8000 Zurich, CH"
    And customer "CU20002" has 1 previous address
    And customer "CU20002" has 1 future address

Scenario: An overlapping address change is rejected
    Given a customer "CU20003" is registered with address "Main Street 1, 8000 Zurich, CH" valid from "2026-06-01"
    When I add a future address "Other Street 2, 8000 Zurich, CH" for customer "CU20003" valid from "2026-03-01"
    Then the address change is rejected because it overlaps the existing address
