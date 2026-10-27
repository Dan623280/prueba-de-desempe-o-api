# Coding style adaptation

Reference: https://github.com/danielalva0004/ApiEmpleados

The user asked to adapt SkillHub to their existing programming style. Keep names and comments in English as originally requested.

- One class per file, including each authentication DTO.
- DTO classes with public properties, default string values and property validation attributes.
- Explicit constructors and private readonly dependencies.
- Conventional method bodies, explicit validation blocks and object initializers.
- Clear service mapping and repository queries with descriptive variables.
- Preserve controllers, library projects, authorization, domain rules, transactions, endpoints and PostgreSQL schema.

The original domain and PostgreSQL integration checks characterize the behavior; rerun them after the adaptation. The Rider session has no C# solution indexed (only Miscellaneous Files); semantic rename preview failed with both relative and absolute declaration paths. Source edits are therefore applied directly and checked with compilation and the existing behavioral suites.
