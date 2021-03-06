﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License. 

namespace Papercut.Events
{
    using System;
    using System.Threading.Tasks;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Infrastructure.MessageBus;

    public class EventPublishAll : IMessageBus
    {
        readonly AutofacMessageBus _autofacMessageBus;

        readonly IEventAggregator _uiEventAggregator;

        public EventPublishAll(
            AutofacMessageBus autofacMessageBus,
            IEventAggregator uiEventAggregator)
        {
            this._autofacMessageBus = autofacMessageBus;
            _uiEventAggregator = uiEventAggregator;
        }

        public async Task Publish<T>([NotNull] T eventObject)
            where T : IEvent
        {
            if (eventObject == null) throw new ArgumentNullException(nameof(eventObject));

            await Task.WhenAll(this._autofacMessageBus.Publish(eventObject), _uiEventAggregator.PublishOnUIThreadAsync(eventObject));
        }
    }
}