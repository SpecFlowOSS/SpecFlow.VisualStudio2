Feature: FullMonty
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

Background: 
	Given there is something in the background

@mytag
Scenario: Add two numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen
	But when I have tables
		| key | value |
		| foo | bar   |
		| boo | boz   |
	And some long text
		"""
			lorem ipsum

			lorem ipsum
		"""

Scenario Outline: A scenario outline
	A long text is provided here

	With empty
	  lines too

	Given something to <what>
	When I <what>
	Then <result> happens

Examples: 
	| what | result |
	| do   | good   |

#this is a comment
@taged @this @too
Examples: bad cases
	| what | result |
	| do   | bad    |

