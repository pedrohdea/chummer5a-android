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

// TIPOS DE WINFORMS REMOVIDOS NO .NET CORE, PARA A SONDAGEM DE scripts/verificar-legado.sh
//
// Não faz parte de nenhum projeto do produto, e mora fora de Chummer/ e de src/ para não ser
// apanhado por nenhum dos dois builds.
//
// POR QUE ISTO PRECISA EXISTIR, E POR QUE UMA LISTA DE EXCEÇÕES NÃO SERVIRIA
//
// A primeira versão de verificar-legado.sh simplesmente ignorava o CS0246 de `ContextMenu`
// como "diferença conhecida entre net48 e net9". Isso parecia razoável e estava errado: o
// erro é de DECLARAÇÃO — um campo e uma propriedade do tipo `ContextMenu` — e enquanto
// houver erro de declaração o Roslyn não vincula corpo de método nenhum (DEC-032).
//
// Ou seja, ignorar o erro na saída não o remove da compilação. O script anunciava "corpos de
// método vinculados" enquanto exatamente nada de corpo era analisado. Provado injetando de
// volta o defeito `DialogResult eShowBPResult = <expressão PromptResult>`, que quebrou o CI
// quatro vezes: com a exceção na lista, o verificador dizia OK.
//
// A diferença precisa ser resolvida no COMPILADOR, não no filtro de saída. Com o tipo
// declarado aqui, os erros de declaração chegam a zero, os corpos passam a ser vinculados, e
// o defeito injetado aparece.
//
// FIDELIDADE
//
// Só o que `SplitButton.cs` usa: o evento `Popup`. Não é uma reimplementação de `ContextMenu`
// e não precisa ser — a sondagem só compila, nunca executa. Se outro membro passar a ser
// usado, o compilador cobra, e é aqui que se acrescenta.

using System;

namespace System.Windows.Forms
{
    /// <summary>
    /// Removida no .NET Core; existe no .NET Framework 4.8, que é o alvo do build legado.
    /// </summary>
    public class ContextMenu : IDisposable
    {
        public event EventHandler Popup;

        /// <summary>
        /// Usado por SplitButton.ShowContextMenuStrip. A sondagem só compila, nunca executa.
        /// </summary>
        public void Show(Control control, System.Drawing.Point pos)
        {
        }

        protected virtual void OnPopup(EventArgs e)
        {
            Popup?.Invoke(this, e);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
