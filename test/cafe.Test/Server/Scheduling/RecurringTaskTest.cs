﻿using System;
using cafe.Server.Scheduling;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace cafe.Test.Server.Scheduling
{
    public class RecurringTaskTest
    {
        private static readonly Duration FiveMinutes = Duration.FromMinutes(5);

        [Fact]
        public void IsReadyToRun_ShouldBeFalseWhenNotPastCreateTime()
        {
            var clock = new FakeClock();
            var recurringTask = CreateRecurringTask(clock, FiveMinutes, CreateFakeScheduledTask);

            recurringTask.IsReadyToRun.Should().BeFalse();
        }

        public static RecurringTask CreateRecurringTask(IClock clock = null, Duration? fiveMinutes = null, Func<IScheduledTask> scheduledTaskCreator = null)
        {
            clock = clock ?? new FakeClock();
            Duration interval = fiveMinutes ?? FiveMinutes;
            scheduledTaskCreator = scheduledTaskCreator ?? CreateFakeScheduledTask;
            return new RecurringTask("task", clock, interval, scheduledTaskCreator);
        }

        [Fact]
        public void IsReadyToRun_ShouldBeTrueWhenPastCreateTime()
        {
            var clock = new FakeClock();

            var recurringTask = CreateRecurringTask(clock, FiveMinutes, CreateFakeScheduledTask);

            clock.AddToCurrentInstant(FiveMinutes);

            recurringTask.IsReadyToRun.Should().BeTrue();
        }

        [Fact]
        public void CreateScheduledTask_ShouldBeFalseAfterFirstRunButBeforeNextScheduled()
        {
            var clock = new FakeClock();
            var expected = new FakeScheduledTask();
            var recurringTask = CreateRecurringTask(clock, FiveMinutes, () => expected);
            clock.AddToCurrentInstant(FiveMinutes);
            var actual = recurringTask.CreateScheduledTask();

            actual.Should().BeSameAs(expected);
        }

        [Fact]
        public void CreateScheduledTask_ShouldThrowExceptionWhenNotReady()
        {
            var recurringTask = CreateRecurringTask(new FakeClock(), FiveMinutes, CreateFakeScheduledTask);
            Assert.Throws<InvalidOperationException>(() => recurringTask.CreateScheduledTask());
        }

        private static IScheduledTask CreateFakeScheduledTask()
        {
            return new FakeScheduledTask();
        }

        [Fact]
        public void IsReadyToRun_ShouldBeFalseAfterCreatingScheduleTaskBeforeNextDurationTime()
        {
            var clock = new FakeClock();
            var recurringTask = CreateRecurringTask(clock, FiveMinutes, CreateFakeScheduledTask);
            clock.AddToCurrentInstant(FiveMinutes);

            recurringTask.CreateScheduledTask();

            recurringTask.IsReadyToRun.Should()
                .BeFalse(
                    "because a task for that time was already created, and the duration since that time hasn't been traversed");
        }

        [Fact]
        public void LastRun_ShouldDefaultToNullBeforeItRuns()
        {
            var recurringTask = CreateRecurringTask(new FakeClock(), FiveMinutes, CreateFakeScheduledTask);
            recurringTask.ToRecurringTaskStatus().LastRun.Should().BeNull("because the recurring task has not yet run");
        }

        [Fact]
        public void ExpectedNextRun_ShouldBeCreatedDatePlusDurationOnInitialRun()
        {
            var interval = FiveMinutes;
            var recurringTask = CreateRecurringTask(new FakeClock(), interval, CreateFakeScheduledTask);

            var recurringTaskStatus = recurringTask.ToRecurringTaskStatus();
            recurringTaskStatus.ExpectedNextRun.Should().Be(recurringTaskStatus.Created.Add(interval.ToTimeSpan()));
        }

        [Fact]
        public void ExpectedNextRun_ShouldBeLastRunDatePlusIntervalAfterItRuns()
        {
            var clock = new FakeClock();
            var interval = FiveMinutes;
            var recurringTask = CreateRecurringTask(clock, interval, CreateFakeScheduledTask);

            clock.AddToCurrentInstant(FiveMinutes);
            recurringTask.CreateScheduledTask();

            var status = recurringTask.ToRecurringTaskStatus();
            status.ExpectedNextRun.Should().Be(status.LastRun.Value.Add(interval.ToTimeSpan()));
        }

        [Fact]
        public void Pause_ShouldMakeTheTaskNotReadyEvenWhenItIsDue()
        {
            var clock = new FakeClock();
            var interval = FiveMinutes;
            var recurringTask = CreateRecurringTask(clock, interval, CreateFakeScheduledTask);

            clock.AddToCurrentInstant(interval);

            recurringTask.Pause();

            recurringTask.IsReadyToRun.Should().BeFalse("because the task is paused");
        }

        [Fact]
        public void Resume_ShouldMakeTheTaskRunnableAgain()
        {
            var clock = new FakeClock();
            var interval = FiveMinutes;
            var recurringTask = CreateRecurringTask(clock, interval, CreateFakeScheduledTask);

            clock.AddToCurrentInstant(interval);

            recurringTask.Pause();
            recurringTask.Resume();

            recurringTask.IsReadyToRun.Should().BeTrue("because the task resumed after pausing");

        }

    }
}