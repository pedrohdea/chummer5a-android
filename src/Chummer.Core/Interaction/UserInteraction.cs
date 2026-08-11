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

using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    /// <summary>
    /// Fachada estática de <see cref="IUserInteraction"/>.
    ///
    /// É estática de propósito. O domínio já chama `Program.ShowMessageBox(...)` estaticamente
    /// em centenas de pontos; manter a forma estática torna a migração desses pontos uma
    /// substituição quase mecânica, em vez de exigir injeção de dependência em classes com
    /// dezenas de milhares de linhas. Mesmo padrão de Timekeeper.ActivityFactory (DEC-025).
    ///
    /// A aplicação instala a implementação no arranque. Sem implementação instalada, todo
    /// prompt devolve o resultado padrão silenciosamente — o que mantém testes e cenários
    /// headless funcionando sem travar esperando um usuário que não existe.
    /// </summary>
    public static class UserInteraction
    {
        /// <summary>
        /// Implementação em uso. Instalada pela camada de apresentação no arranque.
        ///
        /// Nunca é nula: começa como <see cref="SilentUserInteraction"/>. O padrão de objeto
        /// nulo evita anotações de nulabilidade, que este arquivo não pode usar — ele também
        /// é compilado pelo projeto legado net48, preso a C# 7.3 (DEC-027).
        /// </summary>
        public static IUserInteraction Current { get; set; } = new SilentUserInteraction();

        /// <summary>
        /// Resposta devolvida pela implementação silenciosa.
        ///
        /// Não é <see cref="PromptResult.None"/> por acaso: quem chama costuma testar
        /// `== PromptResult.Cancel` para abortar, e devolver Cancel num cenário headless faria
        /// operações legítimas abortarem silenciosamente.
        /// </summary>
        public static PromptResult DefaultResult { get; set; } = PromptResult.OK;

        public static PromptResult ShowMessage(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1)
        {
            return Current.ShowMessage(strMessage, strCaption, eButtons, eIcon, eDefaultButton);
        }

        public static PromptResult ShowScrollableMessage(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1)
        {
            return Current.ShowScrollableMessage(strMessage, strCaption, eButtons, eIcon, eDefaultButton);
        }

        public static Task<PromptResult> ShowMessageAsync(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1,
            CancellationToken token = default)
        {
            return Current.ShowMessageAsync(strMessage, strCaption, eButtons, eIcon, eDefaultButton, token);
        }

        public static Task<PromptResult> ShowScrollableMessageAsync(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1,
            CancellationToken token = default)
        {
            return Current.ShowScrollableMessageAsync(strMessage, strCaption, eButtons, eIcon, eDefaultButton, token);
        }
    }
    /// <summary>
    /// Implementação usada quando ninguém instalou outra: não pergunta nada e devolve
    /// <see cref="UserInteraction.DefaultResult"/>. É o que mantém testes e cenários headless
    /// funcionando sem travar esperando um usuário que não existe.
    /// </summary>
    public sealed class SilentUserInteraction : IUserInteraction
    {
        public PromptResult ShowMessage(string strMessage, string strCaption,
            PromptButtons eButtons, PromptIcon eIcon, PromptDefaultButton eDefaultButton)
        {
            return UserInteraction.DefaultResult;
        }

        public PromptResult ShowScrollableMessage(string strMessage, string strCaption,
            PromptButtons eButtons, PromptIcon eIcon, PromptDefaultButton eDefaultButton)
        {
            return UserInteraction.DefaultResult;
        }

        public Task<PromptResult> ShowMessageAsync(string strMessage, string strCaption,
            PromptButtons eButtons, PromptIcon eIcon, PromptDefaultButton eDefaultButton,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(UserInteraction.DefaultResult);
        }

        public Task<PromptResult> ShowScrollableMessageAsync(string strMessage, string strCaption,
            PromptButtons eButtons, PromptIcon eIcon, PromptDefaultButton eDefaultButton,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(UserInteraction.DefaultResult);
        }
    }
}
