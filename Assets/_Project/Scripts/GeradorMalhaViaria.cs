using System.Collections.Generic;
using _Project.Scripts.Core;
using UnityEngine;

// Define o namespace para organizar nosso código, como planejado.
namespace _Project.Scripts
{
    // Usamos o namespace Core para acessar nossas classes Intersection e Road.
    public class GeradorMalhaViaria : MonoBehaviour
    {
        [Header("Parâmetros do Terreno")]
        public Terrain terreno;

        [Header("Parâmetros de Geração (Random Walker)")]
        public int numeroDePassos = 50; // Antigo "numeroDeIteracoes"
        public float tamanhoDoPasso = 10.0f; // Distância que o agente anda a cada passo
        [Range(0, 45)]
        public float anguloMaximoDeCurva = 15.0f; // O quanto a direção pode mudar a cada passo
        [Range(0, 90)]
        public float inclinacaoMaxima = 30.0f; // Inclinação máxima em graus que uma rua pode ter.

        // Listas para armazenar os dados da nossa malha gerada
        private readonly List<Intersection> _intersecoes = new List<Intersection>();
        private readonly List<Road> _ruas = new List<Road>();
        private int _proximoIdDisponivel;


        [ContextMenu("Gerar Malha Viária")]
        private void Gerar()
        {
            Debug.Log("Iniciando o processo de geração da malha viária...");
            LimparMalhaAnterior();
            ExecutarAlgoritmoRandomWalk();
        }

        [ContextMenu("Limpar Malha Viária")]
        private void LimparMalhaAnterior()
        {
            Debug.Log("Limpando malha anterior...");
            _intersecoes.Clear();
            _ruas.Clear();
            _proximoIdDisponivel = 0;
        }

        private void ExecutarAlgoritmoRandomWalk()
        {
            // Define o ponto de partida no centro do terreno.
            Vector3 posicaoAtual = terreno.transform.position + new Vector3(terreno.terrainData.size.x / 2.0f, 0, terreno.terrainData.size.z / 2.0f);
            // Ajusta a altura inicial para a do terreno.
            posicaoAtual.y = terreno.SampleHeight(posicaoAtual);
            
            // Cria a interseção de origem.
            Intersection intersecaoAnterior = new Intersection(ObterProximoId(), posicaoAtual);
            _intersecoes.Add(intersecaoAnterior);

            // Define uma direção inicial aleatória.
            float anguloAtual = Random.Range(0, 360f);

            // Loop principal para criar cada segmento da estrada.
            for (int i = 0; i < numeroDePassos; i++)
            {
                // Adiciona uma variação aleatória à direção para criar curvas.
                anguloAtual += Random.Range(-anguloMaximoDeCurva, anguloMaximoDeCurva);
                Vector3 direcao = new Vector3(Mathf.Cos(anguloAtual * Mathf.Deg2Rad), 0, Mathf.Sin(anguloAtual * Mathf.Deg2Rad));

                // Calcula a *próxima posição potencial* no plano XZ.
                Vector3 proximaPosicao = posicaoAtual + direcao * tamanhoDoPasso;
                
                // --- INÍCIO DA NOVA LÓGICA DE VERIFICAÇÃO DE INCLINAÇÃO ---

                // Converte a posição no mundo para uma posição normalizada no terreno (valor de 0 a 1).
                float posXNormalizada = (proximaPosicao.x - terreno.transform.position.x) / terreno.terrainData.size.x;
                float posZNormalizada = (proximaPosicao.z - terreno.transform.position.z) / terreno.terrainData.size.z;

                // Pega a inclinação (em graus) no ponto alvo.
                float inclinacao = terreno.terrainData.GetSteepness(posXNormalizada, posZNormalizada);

                // Se a inclinação for maior que o nosso limite, interrompe a geração deste caminho.
                if (inclinacao > inclinacaoMaxima)
                {
                    Debug.Log($"Geração interrompida: inclinação de {inclinacao:F1}° excedeu o máximo de {inclinacaoMaxima}°.");
                    break; // O comando 'break' encerra o loop 'for'.
                }
                
                // --- FIM DA NOVA LÓGICA ---

                // Se a inclinação for aceitável, o processo continua...
                posicaoAtual = proximaPosicao;
                posicaoAtual.y = terreno.SampleHeight(posicaoAtual);

                // Cria a nova interseção (nó) e a armazena.
                Intersection novaIntersecao = new Intersection(ObterProximoId(), posicaoAtual);
                _intersecoes.Add(novaIntersecao);

                // Cria a rua (aresta) conectando a interseção anterior com a nova.
                Road novaRua = new Road(ObterProximoId(), intersecaoAnterior, novaIntersecao);
                _ruas.Add(novaRua);
                
                // Prepara para a próxima iteração.
                intersecaoAnterior = novaIntersecao;
            }

            Debug.Log($"Geração concluída! {_intersecoes.Count} interseções e {_ruas.Count} ruas criadas.");
        }

        // Função auxiliar para garantir IDs únicos
        private int ObterProximoId()
        {
            return _proximoIdDisponivel++;
        }

        // Este método especial do Unity é chamado pelo editor para desenhar Gizmos na tela.
        // É perfeito para visualizar nossa malha sem precisar criar GameObjects.
        private void OnDrawGizmos()
        {
            if (_ruas == null || _ruas.Count == 0)
            {
                return;
            }

            // Desenha as ruas
            Gizmos.color = Color.white;
            foreach (var rua in _ruas)
            {
                Gizmos.DrawLine(rua.StartNode.Position, rua.EndNode.Position);
            }

            // Desenha as interseções
            Gizmos.color = Color.red;
            foreach (var intersecao in _intersecoes)
            {
                Gizmos.DrawSphere(intersecao.Position, 1.0f); // Esfera com raio de 1 metro
            }
        }
    }
}