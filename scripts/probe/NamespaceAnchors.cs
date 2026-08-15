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

// ÂNCORAS DE NAMESPACE PARA A SONDAGEM DO PORTE
//
// Este arquivo NÃO faz parte de nenhum projeto do produto. Ele existe só para as sondagens
// de scripts/censo-erros.sh e scripts/verificar-ui.sh, e mora fora de Chummer/ e de src/
// justamente para não ser apanhado pelos globs de compilação de nenhum dos dois builds.
//
// O PROBLEMA QUE ELE RESOLVE
//
// Quando `using System.Windows.Forms;` não resolve, o Roslyn emite UM erro — o CS0234 da
// própria diretiva — e SUPRIME todos os erros de nome não resolvido no resto do arquivo.
// Medido em repro mínimo: um arquivo com sete linhas e três referências a WinForms reporta
// 1 erro sem âncora e 5 erros com âncora.
//
// O efeito no censo era devastador e silencioso: AddImprovementCollection.cs, com 52
// comparações a DialogResult e 54 usos de ThreadSafeForm, contava como UM erro. O censo
// dizia "89 erros, quase lá" enquanto 28 arquivos escondiam um número desconhecido de
// referências. Pior: fez uma regressão real — a migração para a fachada de interação, que
// quebrou o build legado net48 — passar despercebida na medição em Linux.
//
// COMO FUNCIONA
//
// Um namespace declarado em código-fonte só passa a existir para o `using` se contiver ao
// menos um tipo. Basta então um tipo interno e vazio por namespace: a diretiva resolve, o
// compilador para de colapsar o arquivo, e cada referência real vira o seu próprio erro.
//
// O que NÃO se faz aqui, deliberadamente: declarar os tipos de WinForms (DialogResult,
// TreeNode, Control, ...). O censo mede acoplamento à plataforma; declarar os tipos
// esconderia exatamente aquilo que ele existe para contar. A âncora torna os erros
// visíveis; ela não os faz sumir.
//
// Ver docs/po/decisoes.md, DEC-031.

namespace System.Windows.Forms
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}

namespace Microsoft.ApplicationInsights
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}

namespace Microsoft.ApplicationInsights.Extensibility
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}

namespace Microsoft.ApplicationInsights.DataContracts
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}

namespace RtfPipe
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}

namespace Chummer.Plugins
{
    internal sealed class ProbeNamespaceAnchor
    {
    }
}
