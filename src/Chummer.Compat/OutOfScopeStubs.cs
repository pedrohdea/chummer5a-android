/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

// SUBSISTEMAS FORA DO ESCOPO — stubs que NÃO lançam.
//
// Distinção deliberada em relação a DialogStubs.g.cs: aqueles lançam
// NotSupportedException porque um diálogo atingido em execução é EXATAMENTE a informação
// que o spike de DEC-037 existe para colher. Estes aqui são o contrário — plugins MEF
// (PREM-005) e telemetria do Application Insights (fora do escopo, sem fins comerciais)
// não voltam ao produto. Se lançassem, poluiriam o resultado do spike com falhas que não
// representam trabalho a fazer.
//
// Ver docs/po/decisoes.md, DEC-037 e DEC-048.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer.Plugins
{
    /// <summary>
    /// Superfície de IPlugin que o domínio referencia. MEF está fora do escopo (PREM-005):
    /// nenhuma implementação existe no porte, e PluginLoader devolve null, então estes
    /// membros nunca são chamados.
    /// </summary>
    public interface IPlugin : IDisposable
    {
        Assembly GetPluginAssembly();

        string GetSaveToFileElement(object objCharacter);

        void LoadFileElement(object objCharacter, string strElement);
    }

    /// <summary>Substituto de PluginControl. Nunca instanciado — Program.PluginLoader é null.</summary>
    public sealed class PluginControl : IDisposable
    {
        public IReadOnlyList<IPlugin> MyPlugins => Array.Empty<IPlugin>();

        public IReadOnlyList<IPlugin> MyActivePlugins => Array.Empty<IPlugin>();

        public Task<IReadOnlyList<IPlugin>> GetMyActivePluginsAsync(CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<IPlugin>>(Array.Empty<IPlugin>());

        public void Dispose()
        {
        }
    }
}

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Telemetria está fora do escopo. Todos os membros são no-op para que os blocos
    /// catch do domínio continuem se comportando como no legado.
    /// </summary>
    public sealed class TelemetryClient
    {
        public void TrackException(Exception exception)
        {
        }

        public void TrackException(Exception exception, IDictionary<string, string> properties)
        {
        }

        public void TrackTrace(string message)
        {
        }

        public void TrackEvent(string eventName)
        {
        }

        public void Flush()
        {
        }
    }
}

namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>Telemetria fora do escopo; só a forma da API é preservada.</summary>
    public sealed class TelemetryConfiguration
    {
        public bool DisableTelemetry { get; set; } = true;
    }
}
