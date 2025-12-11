# Automated Test Approaches

A comprehensive automated testing strategy is essential for ensuring code quality, maintainability, and reliability across both frontend and backend.

## Frontend (React + TypeScript)

- **Unit Testing:** Jest, React Testing Library
- **Component Testing:** React Testing Library
- **Integration Testing:** msw for API mocking
- **End-to-End (E2E) Testing:** Cypress (optional)

## Backend (.NET 10, Aspire.NET, EF Core)

- **Unit Testing:** xUnit, Moq, FluentAssertions
- **Component/Integration Testing:** TestServer, Respawn
- **API Testing:** xUnit, FluentAssertions, WebApplicationFactory
- **Test Data:** Builders or test data factories

## General Strategies

- Arrange-Act-Assert (AAA) pattern
- Continuous Integration (CI) with GitHub Actions
- Code coverage monitoring
- Test organization: `frontend/tests/`, `backend/src/tests/`

| Layer     | Frameworks/Libraries                | Test Types                |
|-----------|-------------------------------------|---------------------------|
| Frontend  | Jest, React Testing Library, Cypress| Unit, Component, E2E      |
| Backend   | xUnit, Moq, FluentAssertions, TestServer | Unit, Integration, API   |
