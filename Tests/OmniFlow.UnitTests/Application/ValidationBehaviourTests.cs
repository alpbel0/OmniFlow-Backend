using FluentValidation;
using FluentValidation.Results;
using MediatR;
using OmniFlow.Application.Behaviours;
using OmniFlow.Application.Exceptions;
using ValidationException = OmniFlow.Application.Exceptions.ValidationException;

namespace OmniFlow.UnitTests.Application;

/// <summary>
/// Unit tests for ValidationBehaviour — verifies the MediatR pipeline behaviour
/// correctly intercepts requests before they reach the handler and either:
///   1. Passes through when no validators are registered.
///   2. Passes through when all validators succeed.
///   3. Throws ValidationException (with all error messages) when any validator fails.
/// </summary>
public class ValidationBehaviourTests
{
    // ── Minimal test command/response ──────────────────────────────────────────

    private record TestCommand(string? Name) : IRequest<string>;

    private class PassingValidator : AbstractValidator<TestCommand>
    {
        public PassingValidator() =>
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    private class FailingValidator : AbstractValidator<TestCommand>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Name)
                .Must(_ => false)
                .WithMessage("Always fails.");
        }
    }

    private class NameLengthValidator : AbstractValidator<TestCommand>
    {
        public NameLengthValidator() =>
            RuleFor(x => x.Name).MinimumLength(5).WithMessage("Name must be at least 5 chars.");
    }

    private static RequestHandlerDelegate<string> NextReturning(string value) =>
        _ => Task.FromResult(value);

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WithNoValidators_CallsNextAndReturnsResult()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>([]);
        var command = new TestCommand("hello");

        var result = await behaviour.Handle(command, NextReturning("ok"), default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task WithPassingValidator_CallsNextAndReturnsResult()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new PassingValidator()]);
        var command = new TestCommand("hello");

        var result = await behaviour.Handle(command, NextReturning("ok"), default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task WithFailingValidator_ThrowsValidationException()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new FailingValidator()]);
        var command = new TestCommand("hello");

        var act = () => behaviour.Handle(command, NextReturning("ok"), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task WithFailingValidator_ExceptionContainsErrorMessage()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new FailingValidator()]);
        var command = new TestCommand("hello");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(command, NextReturning("ok"), default));

        ex.Errors.Should().Contain(e => e.Message == "Always fails.");
    }

    [Fact]
    public async Task WithMultipleValidators_AllFailures_AggregatedInException()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new FailingValidator(), new NameLengthValidator()]);
        var command = new TestCommand("ab");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(command, NextReturning("ok"), default));

        // Both validators produced errors — exact count may vary by FluentValidation version
        ex.Errors.Should().Contain(e => e.Message == "Always fails.");
        ex.Errors.Should().Contain(e => e.Message == "Name must be at least 5 chars.");
        ex.Errors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task WithFailingValidator_NextIsNeverCalled()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new FailingValidator()]);
        var command = new TestCommand("hello");
        var nextWasCalled = false;

        RequestHandlerDelegate<string> trackingNext = _ =>
        {
            nextWasCalled = true;
            return Task.FromResult("ok");
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(command, trackingNext, default));

        nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task WithNullNameAndRequiredRule_ThrowsValidationException()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new PassingValidator()]);
        var command = new TestCommand(null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(command, NextReturning("ok"), default));

        ex.Errors.Should().Contain(e => e.Message == "Name is required.");
    }

    [Fact]
    public async Task ValidationException_HasCorrectDefaultMessage()
    {
        var behaviour = new ValidationBehaviour<TestCommand, string>(
            [new FailingValidator()]);
        var command = new TestCommand("hello");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(command, NextReturning("ok"), default));

        ex.Message.Should().Be("One or more validation failures have occurred.");
    }
}
