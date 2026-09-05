Feature: Customer data exchange
    As office staff
    I want to import and export customer master data as JSON or XML files
    So that customer data can be exchanged with other systems

Scenario: Importing a valid JSON file creates the customer
    Given a JSON customer data file with:
      | CustomerNumber | LastName | SurName | Email                  | Street        | HouseNumber | PostalCode | City   | CountryCode | ValidFrom  |
      | CU70001        | Muster   | Hans    | hans.muster@example.ch | Musterstrasse | 10          | 8000       | Zürich | CH          | 2026-01-01 |
    When I import the customer data file
    Then the import succeeds with 1 imported customer
    And customer "CU70001" exists with last name "Muster"

Scenario: Importing a valid XML file creates the customer
    Given an XML customer data file with:
      | CustomerNumber | LastName | SurName | Email                   | Street      | HouseNumber | PostalCode | City   | CountryCode | ValidFrom  |
      | CU70002        | Keller   | Peter   | peter.keller@example.ch | Bahnhofstr. | 5           | 8001       | Zürich | CH          | 2026-01-01 |
    When I import the customer data file
    Then the import succeeds with 1 imported customer
    And customer "CU70002" exists with last name "Keller"

Scenario: A batch containing one invalid customer imports none of them
    Given a JSON customer data file with:
      | CustomerNumber | LastName | SurName  | Email               | Street | HouseNumber | PostalCode | City   | CountryCode | ValidFrom  |
      | CU70003        | Valid    | Customer | valid@example.ch    | Street | 1           | 8000       | Zürich | CH          | 2026-01-01 |
      | not-a-number   | Invalid  | Customer | invalid@example.ch  | Street | 2           | 8000       | Zürich | CH          | 2026-01-01 |
    When I import the customer data file
    Then the import is rejected
    And customer "CU70003" does not exist

Scenario: Importing an already existing customer number is rejected
    Given a customer "CU70004" is registered with address "Existing Street 1, 8000 Zurich, CH" valid from "2026-01-01"
    And a JSON customer data file with:
      | CustomerNumber | LastName  | SurName  | Email                 | Street | HouseNumber | PostalCode | City   | CountryCode | ValidFrom  |
      | CU70004        | Duplicate | Customer | duplicate@example.ch  | Street | 3           | 8000       | Zürich | CH          | 2026-01-01 |
    When I import the customer data file
    Then the import is rejected

Scenario: Historical JSON export reflects the customer's address as of today
    Given a customer "CU70005" is registered with address "Old Street 1, 8000 Zurich, CH" valid from "2020-01-01"
    When I export the customer data as "Json" as of today
    Then the exported file contains customer "CU70005" with address "Old Street 1, 8000 Zurich, CH"

Scenario: Historical XML export reflects the customer's address as of today
    Given a customer "CU70006" is registered with address "Bahnhofstrasse 5, 8001 Zurich, CH" valid from "2020-01-01"
    When I export the customer data as "Xml" as of today
    Then the exported file contains customer "CU70006" with address "Bahnhofstrasse 5, 8001 Zurich, CH"

Scenario: The correct address is selected for a customer who has since moved
    Given a customer "CU70007" is registered with address "Old Street 1, 8000 Zurich, CH" valid from "2020-01-01"
    And customer "CU70007" moved to "New Street 2, 9000 St. Gallen, CH" valid from "2099-01-01"
    When I export the customer data as "Json" as of today
    Then the exported file contains customer "CU70007" with address "Old Street 1, 8000 Zurich, CH"
