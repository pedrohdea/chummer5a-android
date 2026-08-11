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

namespace Chummer
{
    /// <summary>
    /// Which buttons a prompt offers. Equivalente neutro de System.Windows.Forms.MessageBoxButtons.
    /// </summary>
    public enum PromptButtons
    {
        OK,
        OKCancel,
        AbortRetryIgnore,
        YesNoCancel,
        YesNo,
        RetryCancel
    }

    /// <summary>
    /// Severidade do prompt. Equivalente neutro de System.Windows.Forms.MessageBoxIcon.
    /// </summary>
    public enum PromptIcon
    {
        None,
        Information,
        Question,
        Warning,
        Error
    }

    /// <summary>
    /// O que o usuário respondeu. Equivalente neutro de System.Windows.Forms.DialogResult.
    /// </summary>
    public enum PromptResult
    {
        None,
        OK,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No
    }

    /// <summary>
    /// Qual botão vem pré-selecionado. Equivalente neutro de MessageBoxDefaultButton.
    /// </summary>
    public enum PromptDefaultButton
    {
        Button1,
        Button2,
        Button3
    }
}
