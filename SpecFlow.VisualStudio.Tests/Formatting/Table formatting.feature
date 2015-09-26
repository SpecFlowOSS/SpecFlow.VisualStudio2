Feature: Table formatting

Scenario: AutoFormat on tables
	This test includes the following formatting features...
		all table width

Given a valid feature file with the following steps exists:
	"""
	Given the following table exists:
|column 1| column 2|column3   |
	| value information | short    | also something |

	
	When the following table exists:
		|column 1| column 2|column3   |
	| value information | short    | also something |
	"""

When I execute the formatting shortcut

Then the following formatted feature exists:
	"""
	Given the following table exists:
| column 1          | column 2 | column3        |
| value information | short    | also something |

	When the following table exists:
		| column 1          | column 2 | column3        |
		| value information | short    | also something |
	"""
