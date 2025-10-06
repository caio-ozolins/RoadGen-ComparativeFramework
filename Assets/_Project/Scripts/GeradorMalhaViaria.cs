using UnityEngine;

public class GeradorMalhaViaria : MonoBehaviour
{
    [Header("Parâmetros do Terreno")]
    public Terrain terreno; // Arraste o objeto Terreno da cena aqui pelo Inspector.
    public int larguraMundo = 500;
    public int profundidadeMundo = 500;

    [Header("Parâmetros de Geração")]
    public int numeroDeIteracoes = 100;

    // Este atributo cria um botão no Inspector para executar o método.
    // É uma forma excelente de testar a geração sem precisar dar "Play" na cena.
    [ContextMenu("Gerar Malha Viária")]
    private void Gerar()
    {
        Debug.Log("Iniciando o processo de geração da malha viária...");

        // Futuramente, todo o nosso código de geração será chamado a partir daqui.
        LimparMalhaAnterior();

        // (Aqui entrarão as chamadas para os algoritmos)
    }

    [ContextMenu("Limpar Malha Viária")]
    private void LimparMalhaAnterior()
    {
        Debug.Log("Limpando malha anterior...");
        // (Aqui entrará o código para limpar as linhas de debug)
    }
}