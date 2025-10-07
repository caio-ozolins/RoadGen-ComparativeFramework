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
        public int maxTentativasDeDesvio = 5; // N.º de vezes que o agente tenta desviar antes de desistir.

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
            // Ponto de partida no centro do terreno.
            Vector3 posicaoAtual = terreno.transform.position + new Vector3(terreno.terrainData.size.x / 2.0f, 0, terreno.terrainData.size.z / 2.0f);
            posicaoAtual.y = terreno.SampleHeight(posicaoAtual);
            
            Intersection intersecaoAnterior = new Intersection(ObterProximoId(), posicaoAtual);
            _intersecoes.Add(intersecaoAnterior);

            float anguloAtual = Random.Range(0, 360f);
            int tentativasDeDesvio = 0; // Contador para evitar loops infinitos

            for (int i = 0; i < numeroDePassos; i++)
            {
                // Adiciona uma variação aleatória à direção para criar curvas.
                anguloAtual += Random.Range(-anguloMaximoDeCurva, anguloMaximoDeCurva);
                Vector3 direcao = new Vector3(Mathf.Cos(anguloAtual * Mathf.Deg2Rad), 0, Mathf.Sin(anguloAtual * Mathf.Deg2Rad));

                Vector3 proximaPosicao = posicaoAtual + direcao * tamanhoDoPasso;
                
                float posXNormalizada = (proximaPosicao.x - terreno.transform.position.x) / terreno.terrainData.size.x;
                float posZNormalizada = (proximaPosicao.z - terreno.transform.position.z) / terreno.terrainData.size.z;

                float inclinacao = terreno.terrainData.GetSteepness(posXNormalizada, posZNormalizada);

                // --- LÓGICA DE DESVIO ATUALIZADA ---
                if (inclinacao > inclinacaoMaxima)
                {
                    tentativasDeDesvio++; // Incrementa a tentativa
                    
                    if (tentativasDeDesvio >= maxTentativasDeDesvio)
                    {
                        Debug.Log($"Agente desistiu após {maxTentativasDeDesvio} tentativas de desvio.");
                        break; // Desiste se muitas tentativas falharam.
                    }

                    // Força uma curva acentuada para a esquerda ou direita para tentar desviar.
                    anguloAtual += Random.Range(90, 180) * (Random.value > 0.5f ? 1 : -1);
                    
                    i--; // Decrementa o contador do loop para que esta tentativa falha não conte como um "passo".
                    continue; // Pula para a próxima iteração do loop, ignorando a criação da rua.
                }
                
                // Se a inclinação for aceitável, reseta o contador de tentativas e continua.
                tentativasDeDesvio = 0;
                
                posicaoAtual = proximaPosicao;
                posicaoAtual.y = terreno.SampleHeight(posicaoAtual);

                Intersection novaIntersecao = new Intersection(ObterProximoId(), posicaoAtual);
                _intersecoes.Add(novaIntersecao);
                
                Road novaRua = new Road(ObterProximoId(), intersecaoAnterior, novaIntersecao);
                _ruas.Add(novaRua);
                
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