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

using System.Diagnostics;

namespace Chummer
{
    /// <summary>
    /// O que o domínio precisa dizer a uma atividade cronometrada, além de descartá-la.
    ///
    /// Descoberto pelo CI, não por análise: eu havia concluído que o domínio nunca chamava
    /// membros da atividade, com base num grep estreito demais. Character.cs chama
    /// SetSuccess em quatro pontos e escreve metadados de dependência em dois (DEC-025).
    /// </summary>
    public interface ITelemetryActivity
    {
        /// <summary>Marca a operação como bem ou malsucedida.</summary>
        void SetSuccess(bool blnValue);

        /// <summary>Nomeia o tipo e o alvo da operação, para agrupamento no relatório.</summary>
        void SetOperationTarget(string strType, string strTarget);
    }

    /// <summary>
    /// Deixa o domínio falar com a atividade sem conhecer a implementação.
    ///
    /// Quando a atividade não sabe reportar — porque a telemetria está desligada, ou porque
    /// o núcleo roda sem implementação instalada — as chamadas viram no-ops.
    /// </summary>
    public static class TelemetryActivityExtensions
    {
        public static void SetSuccess(this Activity objActivity, bool blnValue)
        {
            // Padrão `is` em vez de `as` + teste de nulo: satisfaz a análise de nulabilidade
            // do net9.0 e continua sendo C# 7.0, dentro do limite de DEC-027.
            if (objActivity is ITelemetryActivity objTelemetry)
                objTelemetry.SetSuccess(blnValue);
        }

        public static void SetOperationTarget(this Activity objActivity, string strType, string strTarget)
        {
            if (objActivity is ITelemetryActivity objTelemetry)
                objTelemetry.SetOperationTarget(strType, strTarget);
        }
    }
}
