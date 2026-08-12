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

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chummer
{
    /// <summary>
    /// Implementação WinForms de <see cref="IUserInteraction"/>, usada pelo aplicativo legado.
    ///
    /// Traduz as solicitações neutras do domínio para as caixas de mensagem existentes,
    /// preservando exatamente o comportamento atual (DEC-026). Quando a UI Avalonia existir,
    /// ela instalará a própria implementação e esta some junto com o projeto legado.
    /// </summary>
    public sealed class WinFormsUserInteraction : IUserInteraction
    {
        public PromptResult ShowMessage(string message, string caption,
            PromptButtons buttons, PromptIcon icon, PromptDefaultButton defaultButton)
        {
            return ToPromptResult(Program.ShowMessageBox(
                message, caption, ToButtons(buttons), ToIcon(icon), ToDefaultButton(defaultButton)));
        }

        public PromptResult ShowScrollableMessage(string message, string caption,
            PromptButtons buttons, PromptIcon icon, PromptDefaultButton defaultButton)
        {
            return ToPromptResult(Program.ShowScrollableMessageBox(
                message, caption, ToButtons(buttons), ToIcon(icon), ToDefaultButton(defaultButton)));
        }

        public async Task<PromptResult> ShowMessageAsync(string message, string caption,
            PromptButtons buttons, PromptIcon icon, PromptDefaultButton defaultButton,
            CancellationToken token)
        {
            return ToPromptResult(await Program.ShowMessageBoxAsync(
                message, caption, ToButtons(buttons), ToIcon(icon),
                ToDefaultButton(defaultButton), token).ConfigureAwait(false));
        }

        public async Task<PromptResult> ShowScrollableMessageAsync(string message, string caption,
            PromptButtons buttons, PromptIcon icon, PromptDefaultButton defaultButton,
            CancellationToken token)
        {
            return ToPromptResult(await Program.ShowScrollableMessageBoxAsync(
                message, caption, ToButtons(buttons), ToIcon(icon),
                ToDefaultButton(defaultButton), token).ConfigureAwait(false));
        }

        public static MessageBoxButtons ToButtons(PromptButtons buttons)
        {
            switch (buttons)
            {
                case PromptButtons.OK: return MessageBoxButtons.OK;
                case PromptButtons.OKCancel: return MessageBoxButtons.OKCancel;
                case PromptButtons.AbortRetryIgnore: return MessageBoxButtons.AbortRetryIgnore;
                case PromptButtons.YesNoCancel: return MessageBoxButtons.YesNoCancel;
                case PromptButtons.YesNo: return MessageBoxButtons.YesNo;
                case PromptButtons.RetryCancel: return MessageBoxButtons.RetryCancel;
                default: throw new ArgumentOutOfRangeException(nameof(buttons));
            }
        }

        public static MessageBoxIcon ToIcon(PromptIcon icon)
        {
            switch (icon)
            {
                case PromptIcon.None: return MessageBoxIcon.None;
                case PromptIcon.Information: return MessageBoxIcon.Information;
                case PromptIcon.Question: return MessageBoxIcon.Question;
                case PromptIcon.Warning: return MessageBoxIcon.Warning;
                case PromptIcon.Error: return MessageBoxIcon.Error;
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        public static MessageBoxDefaultButton ToDefaultButton(PromptDefaultButton eButton)
        {
            switch (eButton)
            {
                case PromptDefaultButton.Button1: return MessageBoxDefaultButton.Button1;
                case PromptDefaultButton.Button2: return MessageBoxDefaultButton.Button2;
                case PromptDefaultButton.Button3: return MessageBoxDefaultButton.Button3;
                default: throw new ArgumentOutOfRangeException(nameof(eButton));
            }
        }

        public static PromptResult ToPromptResult(DialogResult eResult)
        {
            switch (eResult)
            {
                case DialogResult.OK: return PromptResult.OK;
                case DialogResult.Cancel: return PromptResult.Cancel;
                case DialogResult.Abort: return PromptResult.Abort;
                case DialogResult.Retry: return PromptResult.Retry;
                case DialogResult.Ignore: return PromptResult.Ignore;
                case DialogResult.Yes: return PromptResult.Yes;
                case DialogResult.No: return PromptResult.No;
                default: return PromptResult.None;
            }
        }
    }
}
