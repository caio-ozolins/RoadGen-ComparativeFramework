using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;

namespace _Project.Scripts.Generation
{
    // Esta é uma classe C# pura que implementa o "contrato" IGeradorDeMalha.
    public class RandomWalkGerador : IGeradorDeMalha
    {
        // Movemos todos os parâmetros que eram do GeradorMalhaViaria para cá.
        public Terrain Terreno;
        public int NumeroDePassos;
        public float TamanhoDoPasso;
        public float AnguloMaximoDeCurva;
        public float InclinacaoMaxima;
        public int MaxTentativasDeDesvio;

        private int _proximoIdDisponivel;

        // O método Gerar agora retorna o resultado, em vez de guardá-lo em listas da classe.
        public (List<Intersection> intersecoes, List<Road> ruas) Gerar()
        {
            var intersecoes = new List<Intersection>();
            var ruas = new List<Road>();
            _proximoIdDisponivel = 0;

            // A lógica abaixo é a mesma que tínhamos antes, apenas adaptada para esta classe.
            Vector3 posicaoAtual = Terreno.transform.position + new Vector3(Terreno.terrainData.size.x / 2.0f, 0, Terreno.terrainData.size.z / 2.0f);
            posicaoAtual.y = Terreno.SampleHeight(posicaoAtual);

            Intersection intersecaoAnterior = new Intersection(ObterProximoId(), posicaoAtual);
            intersecoes.Add(intersecaoAnterior);

            float anguloAtual = Random.Range(0, 360f);
            int tentativasDeDesvio = 0;

            for (int i = 0; i < NumeroDePassos; i++)
            {
                anguloAtual += Random.Range(-AnguloMaximoDeCurva, AnguloMaximoDeCurva);
                Vector3 direcao = new Vector3(Mathf.Cos(anguloAtual * Mathf.Deg2Rad), 0, Mathf.Sin(anguloAtual * Mathf.Deg2Rad));
                Vector3 proximaPosicao = posicaoAtual + direcao * TamanhoDoPasso;

                float posXNormalizada = (proximaPosicao.x - Terreno.transform.position.x) / Terreno.terrainData.size.x;
                float posZNormalizada = (proximaPosicao.z - Terreno.transform.position.z) / Terreno.terrainData.size.z;
                float inclinacao = Terreno.terrainData.GetSteepness(posXNormalizada, posZNormalizada);

                if (inclinacao > InclinacaoMaxima)
                {
                    tentativasDeDesvio++;
                    if (tentativasDeDesvio >= MaxTentativasDeDesvio)
                    {
                        Debug.Log($"Agente desistiu após {MaxTentativasDeDesvio} tentativas de desvio.");
                        break;
                    }
                    anguloAtual += Random.Range(90, 180) * (Random.value > 0.5f ? 1 : -1);
                    i--;
                    continue;
                }

                tentativasDeDesvio = 0;
                posicaoAtual = proximaPosicao;
                posicaoAtual.y = Terreno.SampleHeight(posicaoAtual);

                Intersection novaIntersecao = new Intersection(ObterProximoId(), posicaoAtual);
                intersecoes.Add(novaIntersecao);
                Road novaRua = new Road(ObterProximoId(), intersecaoAnterior, novaIntersecao);
                ruas.Add(novaRua);
                intersecaoAnterior = novaIntersecao;
            }

            Debug.Log($"[RandomWalkGerador] Geração concluída! {intersecoes.Count} interseções e {ruas.Count} ruas criadas.");

            return (intersecoes, ruas);
        }
        
        private int ObterProximoId()
        {
            return _proximoIdDisponivel++;
        }
    }
}