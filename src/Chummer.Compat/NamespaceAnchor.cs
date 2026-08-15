// Âncora para que `using System.Windows.Forms;` resolva no alvo Android.
//
// Os 27 arquivos do Backend que ainda têm essa diretiva colapsam num único erro sem ela, e o
// Roslyn para de vincular os corpos (DEC-032) — escondendo exatamente o acoplamento que este
// spike existe para medir. Declarar o namespace vazio faz cada referência real voltar a ter
// o seu próprio erro.
//
// Os TIPOS de WinForms não são declarados aqui de propósito: quem precisa deles é o
// DialogStubs.g.cs, no namespace Chummer, que é onde o domínio os resolve.

namespace System.Windows.Forms
{
    internal sealed class CompatNamespaceAnchor
    {
    }
}
