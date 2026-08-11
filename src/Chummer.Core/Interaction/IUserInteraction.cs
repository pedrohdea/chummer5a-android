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
    /// Como o domínio fala com o usuário.
    ///
    /// Por que isto existe: as regras de Shadowrun genuinamente precisam perguntar ao usuário
    /// no meio de um cálculo — "escolha um atributo para receber +1", "confirma remover esta
    /// qualidade?". Hoje o domínio resolve isso construindo formulários WinForms diretamente
    /// (329 chamadas a MessageBox e ~120 instanciações de diálogos de seleção dentro de
    /// Backend/). Isso não é um defeito acidental a ser eliminado: a pergunta é regra de jogo.
    ///
    /// O que muda é QUEM responde. O domínio emite uma solicitação; a camada de apresentação
    /// decide como apresentá-la — MessageBox no WinForms legado, folha modal no Android.
    ///
    /// Ver docs/po/decisoes.md, DEC-026.
    /// </summary>
    public interface IUserInteraction
    {
        /// <summary>
        /// Versão síncrona de <see cref="ShowMessageAsync"/>.
        ///
        /// Existe porque o domínio tem 73 pontos de chamada síncronos, e convertê-los a
        /// async agora misturaria a extração do núcleo (Etapa 2) com o saneamento do modelo
        /// assíncrono (Etapa 5). A dualidade é deliberada e temporária: a Etapa 5 remove a
        /// metade síncrona junto com as outras 279 execuções síncronas de código async.
        /// Ver DEC-028.
        /// </summary>
        PromptResult ShowMessage(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1);

        /// <summary>
        /// Versão síncrona de <see cref="ShowScrollableMessageAsync"/>. Ver DEC-028.
        /// </summary>
        PromptResult ShowScrollableMessage(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1);

        /// <summary>
        /// Mostra uma mensagem e devolve o que o usuário respondeu.
        /// </summary>
        Task<PromptResult> ShowMessageAsync(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1,
            CancellationToken token = default);

        /// <summary>
        /// Igual a <see cref="ShowMessageAsync"/>, mas para textos longos que precisam rolar —
        /// relatórios de validação, listas de itens afetados.
        /// </summary>
        Task<PromptResult> ShowScrollableMessageAsync(
            string strMessage,
            string strCaption = "",
            PromptButtons eButtons = PromptButtons.OK,
            PromptIcon eIcon = PromptIcon.None,
            PromptDefaultButton eDefaultButton = PromptDefaultButton.Button1,
            CancellationToken token = default);
    }
}
