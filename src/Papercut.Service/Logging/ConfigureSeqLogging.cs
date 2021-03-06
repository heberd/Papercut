﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2019 Jaben Cargman
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


namespace Papercut.Service.Logging
{
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Common.Helper;
    using Papercut.Core.Infrastructure.Logging;
    using Papercut.Service.Helpers;

    using Serilog;

    public class ConfigureSeqLogging : IEventHandler<ConfigureLoggerEvent>
    {
        private readonly PapercutServiceSettings _settings;

        public ConfigureSeqLogging(PapercutServiceSettings settings)
        {
            this._settings = settings;
        }

        public Task Handle(ConfigureLoggerEvent @event)
        {
            if (this._settings.SeqEndpoint.IsSet())
            {
                @event.LogConfiguration.WriteTo.Seq(_settings.SeqEndpoint);
            }

            return Task.CompletedTask;
        }
    }
}