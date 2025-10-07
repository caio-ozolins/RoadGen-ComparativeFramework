using System.Collections.Generic;
using _Project.Scripts.Core;

namespace _Project.Scripts.Generation.Abstractions
{
    /// <summary>
    /// Define o "contrato" que todo algoritmo de geração de malha viária deve seguir.
    /// Garante que todos os geradores terão um método Gerar() que retorna o resultado da malha.
    /// </summary>
    public interface IGeradorDeMalha
    {
        // Neste caso, ele retorna a lista de interseções e a lista de ruas geradas.
        (List<Intersection> intersecoes, List<Road> ruas) Gerar();
    }
}