﻿using SharpTestsEx;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Threading.Tasks;

namespace MediatR.Tests.Publishing
{
    public class SynchronousHandler
    {
        class ServiceLocator
        {
            private readonly Dictionary<Type, List<object>> Services = new Dictionary<Type, List<object>>();

            public void Register(Type type, params object[] implementations)
                => Services.Add(type, implementations.ToList());

            public List<object> Get(Type type) { return Services[type]; }
        }

        class TasksList
        {
            public List<string> Tasks { get; }

            public TasksList()
            {
                Tasks = new List<string>();
            }
        }

        class TaskWasAdded : INotification
        {
            public string TaskName { get; }

            public TaskWasAdded(string taskName)
            {
                TaskName = taskName;
            }
        }

        class TaskWasAddedHandler : INotificationHandler<TaskWasAdded>
        {
            private readonly TasksList _taskList;
            public TaskWasAddedHandler(TasksList tasksList)
            {
                _taskList = tasksList;
            }

            public void Handle(TaskWasAdded @event)
            {
                _taskList.Tasks.Add(@event.TaskName);
            }
        }

        private readonly IMediator mediator;
        private readonly TasksList _tasksList = new TasksList();

        public SynchronousHandler()
        {
            var notificationHandler = new TaskWasAddedHandler(_tasksList);

            var serviceLocator = new ServiceLocator();

            serviceLocator.Register(typeof(INotificationHandler<TaskWasAdded>), notificationHandler);
            //Registration needed internally by MediatR
            serviceLocator.Register(typeof(IAsyncNotificationHandler<TaskWasAdded>), new IAsyncNotificationHandler<TaskWasAdded>[] { });
            serviceLocator.Register(typeof(ICancellableAsyncNotificationHandler<TaskWasAdded>), new ICancellableAsyncNotificationHandler<TaskWasAdded>[] { });

            mediator = new Mediator(
                    type => serviceLocator.Get(type).FirstOrDefault(),
                    type => serviceLocator.Get(type));
        }

        [Fact]
        public async void GivenRegisteredhronousRequestHandler_WhenPublishMethodIsBeingCalled_ThenReturnsProperResult()
        {
            //Given
            var @event = new TaskWasAdded("cleaning");

            //When
            await mediator.Publish(@event);

            //Then
            _tasksList.Tasks.Should().Have.Count.EqualTo(1);
            _tasksList.Tasks.Should().Have.SameValuesAs("cleaning");
        }
    }
}
