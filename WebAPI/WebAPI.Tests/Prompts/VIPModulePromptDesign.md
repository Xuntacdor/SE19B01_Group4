## Test Strategy for VIP Module (Controllers + Services)

- Objectives: >90% line/branch coverage across `VipPlanController`, `VipPaymentController`, `VipPlanService`, and `VipAuthorizationService`.
- Scope:
  - Controller paths: success, not found, validation errors, auth claims logic.
  - Service logic: mapping correctness, input validation, update/delete branches, VIP eligibility time logic.

### VipPlanController
- GetAll: returns 200 with list payload.
- GetById: 200 when found; 404 when null.
- Create: 201 with CreatedAtAction and returned dto.
- Update: 200 when updated; 404 when missing.
- Delete: 204 when deleted; 404 when missing.

### VipPaymentController
- CreateVipCheckout:
  - 200 when userId from claims; asserts URL value.
  - 200 when userId from body fallback.
  - 400 when missing userId.
  - 400 when invalid plan id (<= 0).

### VipPlanService
- GetAll: maps entity -> dto correctly.
- GetById: returns null when not found.
- Create: throws on empty name; persists and returns dto on valid input.
- Update: returns null when not found; updates fields and saves when found.
- Delete: returns false when not found; deletes and returns true when found.

### VipAuthorizationService
- IsUserVip: false on missing or expired; true on active.
- GetVipExpireDate: MinValue when missing/null; returns exact date otherwise.
- EnsureVipAccess: throws UnauthorizedAccessException when not VIP.
- GetVipStatus: variants for not found, expired, and active including days remaining.

### Notes
- Use Moq to isolate repositories/services.
- Use FluentAssertions for expressive expectations.
- Avoid external dependencies; focus on deterministic paths.
# VIP Module Test Prompts (Payment, Plan, Authorization)

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic VIP module (.NET 8, xUnit, Moq, FluentAssertions) — handles VIP plans, payments, webhooks, and authorization checks.  
Architecture: Controller → Service → External Services (Stripe) → Repository.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, payment processing, webhook handling, or exception handling.  
- List mockable dependencies (e.g., `IVipPlanRepository`, `ITransactionRepository`, `IStripePaymentService`, `IStripeWebhookService`, `IVipAuthorizationService`).  
- Mention edge cases (null inputs, invalid data, exceptions, payment failures, expired subscriptions).  
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
IELTSPhobic VIP module (.NET 8 WebAPI + xUnit) — integrates with Stripe for payment processing and manages VIP subscription features.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (`VipPlanController`, `VipPaymentController`, `StripeWebhookController`, `VipAuthorizationHandler`, `VipPlanService`, `StripePaymentService`, `StripeWebhookService`, `VipAuthorizationService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `StripeClient`, `IVipPlanRepository`, `ITransactionRepository`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid payment When processed | payment data | Returns success |
| Edge Case | Given expired plan When checking access | expired date | Returns unauthorized |
| Error | Given invalid webhook When received | invalid signature | Returns bad request |
| Integration | Given valid webhook When processed | stripe event | Updates user status |

**Output Title:**  
`### AI Output: Test Cases Matrix for VipPlanService`

## Phase 3 – Code Generation

### Prompt 1 – VipPlanController & VipPlanService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `VipPlanController` and `VipPlanService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `VipPlanController`, mock: `IVipPlanService`, `ILogger<VipPlanController>`.
- For `VipPlanService`, mock: `IVipPlanRepository`, `ITransactionRepository`.
- Test all main public methods:
  - VipPlanController: `GetAll`, `GetById`, `GetByType`, `Create`, `Update`, `Delete`
  - VipPlanService: `GetAll`, `GetById`, `GetByType`, `Create`, `Update`, `Delete`, `GetActivePlans`
- Test plan pricing, features, and activation logic.
- Include both success and failure scenarios (BadRequest, NotFound, Exception).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – VipPaymentController & VipPaymentService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `VipPaymentController`, `VipPaymentService`, and `StripePaymentService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `PaymentIntentService`, `CustomerService` from Stripe, and `IVipPlanRepository`, `ITransactionRepository`.
- Test all main public methods:
  - VipPaymentController: `CreateCheckoutSession`, `HandlePaymentSuccess`, `HandlePaymentFailure`
  - VipPaymentService: `CreateCheckoutAsync`, `ProcessPaymentAsync`, `RefundPaymentAsync`
  - StripePaymentService: `CreatePaymentIntent`, `CreateCustomer`, `UpdateSubscription`
- Test payment flow, session creation, and error handling.
- Mock Stripe API responses (both success and failure cases).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – StripeWebhookController & StripeWebhookService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `StripeWebhookController` and `StripeWebhookService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `StripeWebhookController`, mock: `IStripeWebhookService`, `ILogger<StripeWebhookController>`, `HttpContext`.
- For `StripeWebhookService`, mock: `IUserRepository`, `ITransactionRepository`, `IVipPlanRepository`.
- Test all main public methods:
  - StripeWebhookController: `HandleWebhook`
  - StripeWebhookService: `ProcessWebhook`, `HandlePaymentIntent`, `HandleSubscriptionUpdate`, `HandleInvoicePayment`
- Test webhook signature validation, event processing, and database updates.
- Simulate different webhook events (payment_intent.succeeded, customer.subscription.updated, etc.).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 4 – VipAuthorizationHandler & VipAuthorizationService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `VipAuthorizationHandler` and `VipAuthorizationService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `VipAuthorizationHandler`, mock: `IVipAuthorizationService`, `AuthorizationHandlerContext`, `HttpContext`.
- For `VipAuthorizationService`, mock: `IUserRepository`, `IVipPlanRepository`, `ITransactionRepository`.
- Test all main public methods:
  - VipAuthorizationHandler: `HandleRequirementAsync`
  - VipAuthorizationService: `CheckVipAccess`, `IsVipActive`, `GetVipStatus`
- Test authorization checks for VIP-only features.
- Test expired subscriptions, inactive users, and plan restrictions.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 5 – VipPlanRepository
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `VipPlanRepository` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

Mock dependencies: `ApplicationDbContext` and its `DbSet<VipPlan>`.

**Test all main public methods:**
For the repository — `GetAll`, `GetById`, `GetByType`, `Add`, `Update`, `Delete`.

Include helper methods such as `CreateSampleVipPlan()` and `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in ```csharp ... ``` with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for VipPlanRepository.

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
I'm testing the VIP module services in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.


