# Transaction Module Test Prompts (Payment, Webhook, Service, Controller, Repository)

## Test Strategy for Transaction Module (Controllers + Services)

- Objectives: >90% line/branch coverage across `TransactionController`, `StripeWebhookController`, `TransactionService`, `StripePaymentService`, and `StripeWebhookService`, plus core repository functions in `TransactionRepository`.
- Scope:
  - Controller paths: success, not found, validation errors, auth claims/session logic.
  - Service logic: validation, mapping, calculations, refund paths, CSV export branches, pagination/sorting/filtering.
  - Payment/webhook flows: Stripe checkout session creation, event processing, zero-decimal currency handling.

### TransactionController
- GetPaged: 200 with paginated payload (admin vs user scope); 401 when no auth.
- GetById: 200 when found; 404 when null; 401 when no auth.
- Post: 201 CreatedAt when created or found by ref; 401 when no auth.
- Refund: 200 on success; 409 on service exception; 401 when no auth.
- CreateTransaction: 200 on success; 401 when no auth.
- Export: File (CSV) on success; 401 when no auth.

### StripeWebhookController
- HandleAsync: 400 on invalid signature; 400 when service throws; 500 when secret missing.

### TransactionService
- GetById: null when not found/forbidden; returns DTO for admin or owner.
- CreateOrGetByReference: validates inputs; reuses existing by ProviderTxnId; creates new otherwise.
- Refund: validates permissions; updates status; throws on bad cases.
- CreateVipTransaction: validates plan; creates pending transaction.
- ExportCsv: covers CSV escaping and column composition.
- GetPaged: covers filters, sorts, admin vs user scope.

### StripePaymentService
- CreateVipCheckoutSession: plan check, config URL building, currency handling, SessionService call; returns checkout URL or empty.
- IsZeroDecimalCurrency: true for VND/JPY; false for USD/EUR.
- GetUrl: domain + fallback when explicit URL not configured.

### StripeWebhookService
- ProcessWebhook: routes by event type; updates user and transaction states; idempotency via GetByReference.
- IsZeroDecimal: static currency check.
- TryGetInt: parse helper for metadata.

### Notes
- Use Moq to isolate repositories/services.
- Use FluentAssertions for expressive expectations.
- Avoid external network calls; mock Stripe services and repositories.

---

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Transaction module (.NET 8, xUnit, Moq, FluentAssertions) — handles payments (Stripe), transactions, webhooks, and exporting.  
Architecture: Controller → Service → External Services (Stripe) → Repository.

**Role:**  
Expert software test engineer and prompt engineer.

**Goal:**  
Identify all public/business-logic methods that need unit or integration testing (>80% coverage).

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, payment processing, webhook handling, export, pagination, exception handling.  
- List mockable dependencies (e.g., `ITransactionRepository`, `IVipPlanRepository`, `IUserRepository`, `SessionService`, `IStripeWebhookService`).  
- Mention edge cases (null inputs, invalid data, exceptions, payment failures, unauthorized access).

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

---

## Phase 2 – Test Case Matrix

**Context:**  
IELTSPhobic Transaction module (.NET 8 WebAPI + xUnit) — integrates with Stripe for payment processing and manages transaction history, refunds, and CSV export.

**Role:**  
Expert .NET test engineer.

**Goal:**  
Generate a Test Case Matrix (>80% coverage) for all public methods.

**Instructions:**  
1. Analyze classes (`TransactionController`, `StripeWebhookController`, `TransactionService`, `StripePaymentService`, `StripeWebhookService`, `TransactionRepository`).  
2. List 4–6 test cases per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use Given–When–Then format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `SessionService`, `IVipPlanRepository`, `ITransactionRepository`).

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid plan When creating checkout | planId=1,userId=2 | Returns checkout URL |
| Edge Case | Given missing plan When creating checkout | plan not found | Throws InvalidOperation |
| Error | Given webhook invalid signature | signature="invalid" | Returns BadRequest |
| Integration | Given payment completed When processed | event=checkout.session.completed | Updates user, saves txn |

**Output Title:**  
`### AI Output: Test Cases Matrix for TransactionService`

---

## Phase 3 – Code Generation

### Prompt 1 – TransactionController & TransactionService
You are an expert .NET test engineer.

Generate fully compilable xUnit test code (C#) for the `TransactionController` and `TransactionService` classes in the IELTSPhobic web application.  
Do not create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `TransactionController`, mock: `ITransactionService`.
- For `TransactionService`, mock: `ITransactionRepository`, `IVipPlanRepository`, `IUserRepository`.
- Test all main public methods:
  - TransactionController: `GetPaged`, `GetById`, `Post`, `Refund`, `CreateTransaction`, `Export`
  - TransactionService: `GetById`, `CreateOrGetByReference`, `Refund`, `CreateVipTransaction`, `ExportCsv`, `GetPaged`
- Include both success and failure scenarios (Unauthorized, NotFound, Conflict, Exception).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**  
Produce clean, syntactically correct test code that can compile under .NET 8.

---

### Prompt 2 – StripePaymentService
You are an expert .NET test engineer.

Generate fully compilable xUnit test code (C#) for the `StripePaymentService` class in the IELTSPhobic web application.  
Do not create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock: `IConfiguration`, `IVipPlanRepository`, `SessionService`.
- Test all main public methods: `CreateVipCheckoutSession` and the static/hidden helpers via reflection if needed (`IsZeroDecimalCurrency`, `GetUrl`).
- Mock Stripe session creation (success, null URL, exception branches).
- Each `[Fact]` must be a standalone public test method.
- Output as a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**  
Produce clean, syntactically correct test code that can compile under .NET 8.

---

### Prompt 3 – StripeWebhookController & StripeWebhookService
You are an expert .NET test engineer.

Generate fully compilable xUnit test code (C#) for the `StripeWebhookController` and `StripeWebhookService` classes in the IELTSPhobic web application.  
Do not create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `StripeWebhookController`, mock: `IStripeWebhookService`, `ILogger<StripeWebhookController>`, `HttpContext`.
- For `StripeWebhookService`, mock: `IUserRepository`, `ITransactionRepository`, `IVipPlanRepository`.
- Test all main public methods:
  - StripeWebhookController: `HandleAsync`
  - StripeWebhookService: `ProcessWebhook` (and internal routing handlers by event type)
- Validate signature handling, event routing, status updates, idempotency by `GetByReference`.
- Simulate different webhook events (checkout.session.completed, payment_intent.canceled, charge.refunded, etc.).
- Each `[Fact]` must be a standalone public test method.
- Output as a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**  
Produce clean, syntactically correct test code that can compile under .NET 8.

---

### Prompt 4 – TransactionRepository
You are an expert .NET test engineer.

Generate fully compilable xUnit test code (C#) for the `TransactionRepository` class in the IELTSPhobic web application.  
Do not create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**  
Use `xUnit`, `Moq`, and `FluentAssertions`.

Mock dependencies: `ApplicationDbContext` (or use InMemory provider) and its `DbSet<Transaction>`.

**Test all main public methods:**
- `GetAll`, `GetById`, `GetByReference`, `IncludeUserAndPlan`, `Add`, `Update`.

Include helpers such as `CreateSampleTransaction()` and `SetupMockRepository()` or use EF InMemory as appropriate.  
Use realistic data and assertions that fully compile under .NET 8.  
Each `[Fact]` must be a standalone public test method.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for TransactionRepository.

---

## Phase 4 – Bug Fixes and Coverage

### Fix Failing Tests
You are an expert .NET test engineer.  
I will give you a C# test class and the `dotnet test` output.  
Analyze the failing tests and explain why each failed.  
Determine whether the issue is in the test expectations or in the controller/service logic.  
Then show the exact corrected code snippet for each failed test.

**Format:**
1. Reason for failure
2. Corrected code snippet (test or controller/service)

### Increase Coverage
I'm testing the Transaction module in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 75%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 85%.

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.
