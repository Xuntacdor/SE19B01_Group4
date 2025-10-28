# Forum Module Test Prompts (Comment, Post, Tag)

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Forum module (.NET 8, xUnit, Moq, FluentAssertions) — handles forum posts, comments, tags, and moderation.  
Architecture: Controller → Service → Repository → Database.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, business logic, or exception handling.  
- List mockable dependencies (e.g., `IPostRepository`, `ICommentRepository`, `ITagRepository`, `IUserRepository`).  
- Mention edge cases (null inputs, invalid data, exceptions, authorization checks, moderation).  
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
IELTSPhobic Forum module (.NET 8 WebAPI + xUnit) — handles post creation, commenting, tagging, and content moderation.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (`PostController`, `CommentController`, `TagController`, `PostService`, `CommentService`, `TagService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `IPostRepository`, `ICommentRepository`, `ITagRepository`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid post When created | post data | Returns created post |
| Edge Case | Given null title When created | title=null | Returns BadRequest |
| Error | Given duplicate tag | existing tag | Returns conflict |
| Integration | Given valid post When saved | mocks active | Calls repository once |

**Output Title:**  
`### AI Output: Test Cases Matrix for PostService`

## Phase 3 – Code Generation

### Prompt 1 – PostController & PostService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `PostController` and `PostService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `PostController`, mock: `IPostService`, `ILogger<PostController>`.
- For `PostService`, mock: `IPostRepository`, `ITagRepository`, `IUserRepository`.
- Test all main public methods:
  - PostController: `GetAll`, `GetById`, `GetByTag`, `GetByUser`, `Create`, `Update`, `Delete`, `Approve`, `Reject`
  - PostService: `GetAll`, `GetById`, `GetByTag`, `GetByUser`, `Create`, `Update`, `Delete`, `Moderate`
- Test pagination, filtering, moderation workflows.
- Include both success and failure scenarios (BadRequest, NotFound, Unauthorized).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – CommentController & CommentService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `CommentController` and `CommentService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `CommentController`, mock: `ICommentService`, `ILogger<CommentController>`.
- For `CommentService`, mock: `ICommentRepository`, `IPostRepository`, `IUserRepository`.
- Test all main public methods:
  - CommentController: `GetAll`, `GetByPost`, `GetById`, `Create`, `Update`, `Delete`
  - CommentService: `GetAll`, `GetByPost`, `GetById`, `Create`, `Update`, `Delete`
- Test comment threading, nested replies, moderation.
- Include edge cases like empty comments, invalid post IDs.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – TagController & TagService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `TagController` and `TagService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `TagController`, mock: `ITagService`, `ILogger<TagController>`.
- For `TagService`, mock: `ITagRepository`.
- Test all main public methods:
  - TagController: `GetAll`, `GetById`, `GetByName`, `Create`, `Update`, `Delete`
  - TagService: `GetAll`, `GetById`, `GetByName`, `Create`, `Update`, `Delete`
- Test tag creation, update, deletion, and relationships with posts.
- Include edge cases like duplicate tags, invalid names.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 4 – Forum Repositories
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `PostRepository`, `CommentRepository`, and `TagRepository` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

Mock dependencies: `ApplicationDbContext` and its `DbSet<Post>`, `DbSet<Comment>`, `DbSet<Tag>`.

**Test all main public methods:**
For the repositories — `GetAll`, `GetById`, `Add`, `Update`, `Delete`, `GetByTag`, `GetByUser`, etc.

Include helper methods such as `CreateSamplePost()`, `CreateSampleComment()`, `CreateSampleTag()`, `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in ```csharp ... ``` with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for all repositories.

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
I'm testing the PostService, CommentService, and TagService classes in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.


