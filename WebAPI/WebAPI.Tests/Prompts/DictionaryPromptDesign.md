# Dictionary Module Test Prompts (Word, VocabGroup)

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Dictionary module (.NET 8, xUnit, Moq, FluentAssertions) — handles word definitions, vocabulary groups, and dictionary API integration.  
Architecture: Controller → Service → External API (Dictionary) → Repository.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, external API calls, or exception handling.  
- List mockable dependencies (e.g., `IWordRepository`, `IVocabGroupRepository`, `DictionaryApiClient`).  
- Mention edge cases (null inputs, invalid data, exceptions, API failures, duplicate entries).  
- Suggest both **happy-path** and **failure** test cases.  

**Output Format (Markdown):**  
```markdown
### Functions to Test
1. **FunctionName(params)**
   - **Main Purpose:** one-line summary  
   - **Inputs:** type + meaning  
   - **Returns:** type + purpose  
   - **Dependencies to Mock:**  
   - **Edge Cases:**  
   - **Suggested Test Names:** Given_When_Then...
```

## Phase 2 – Test Case Matrix

**Context:**  
IELTSPhobic Dictionary module (.NET 8 WebAPI + xUnit) — integrates with external dictionary APIs and manages vocabulary groups.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (`WordController`, `VocabGroupController`, `WordService`, `VocabGroupService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `DictionaryApiClient`, `IWordRepository`, `IVocabGroupRepository`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid word When searched | word="test" | Returns definition |
| Edge Case | Given null word When searched | word=null | Returns BadRequest |
| Error | Given API fails | external API down | Returns 500 |
| Integration | Given valid word When stored | word data | Saves to database |

**Output Title:**  
`### AI Output: Test Cases Matrix for WordService`

## Phase 3 – Code Generation

### Prompt 1 – WordController & WordService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `WordController` and `WordService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `WordController`, mock: `IWordService`, `ILogger<WordController>`.
- For `WordService`, mock: `IWordRepository`, `DictionaryApiClient`.
- Test all main public methods:
  - WordController: `GetAll`, `GetById`, `Search`, `Create`, `Update`, `Delete`
  - WordService: `GetAll`, `GetById`, `Search`, `GetDefinition`, `Create`, `Update`, `Delete`
- Test word search, definition retrieval, and external API integration.
- Include both success and failure scenarios (BadRequest, NotFound, API errors).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – VocabGroupController & VocabGroupService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `VocabGroupController` and `VocabGroupService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `VocabGroupController`, mock: `IVocabGroupService`, `ILogger<VocabGroupController>`.
- For `VocabGroupService`, mock: `IVocabGroupRepository`, `IWordRepository`.
- Test all main public methods:
  - VocabGroupController: `GetAll`, `GetById`, `GetByLevel`, `Create`, `Update`, `Delete`, `AddWord`, `RemoveWord`
  - VocabGroupService: `GetAll`, `GetById`, `GetByLevel`, `Create`, `Update`, `Delete`, `AddWordToGroup`, `RemoveWordFromGroup`
- Test vocabulary group management and word associations.
- Include edge cases like duplicate words, invalid groups.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – WordRepository & VocabGroupRepository
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `WordRepository` and `VocabGroupRepository` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

Mock dependencies: `ApplicationDbContext` and its `DbSet<Word>`, `DbSet<VocabGroup>`.

**Test all main public methods:**
For the repositories — `GetAll`, `GetById`, `Add`, `Update`, `Delete`, `Search`, `GetByLevel`, etc.

Include helper methods such as `CreateSampleWord()`, `CreateSampleVocabGroup()`, `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in ```csharp ... ``` with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for both repositories.

### Prompt 4 – DictionaryApiClient
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `DictionaryApiClient` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `HttpClient` and `IConfiguration`.
- Test all main public methods:
  - DictionaryApiClient: `GetDefinition`, `SearchWord`, `GetSynonyms`
- Test successful API calls, failed API calls, and timeout scenarios.
- Mock HTTP responses for different status codes (200, 404, 500).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

## Phase 4 – Bug Fixes and Coverage

### Fix Failing Tests
You are an expert .NET test engineer.
I will give you a C# test class and the `dotnet test` output.
Analyze the failing tests and explain why each failed.
Determine whether the issue is in the test expectations or in the controller/service logic.
Then show the exact corrected code snippet for each failed test.

**Format:**
1. Reason for failure
2. Corrected code snippet (test or controller)

### Increase Coverage
I'm testing the WordService and VocabGroupService classes in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.


