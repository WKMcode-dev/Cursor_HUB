# Cursor Hub — guia técnico

[← Apresentação e download](../README.md)

Aplicativo para Windows x64, desenvolvido em C# com .NET 10, osu!framework, Svg.Skia e recursos gráficos em SVG/PNG. A release oficial atual é a **v0.10.1**.

## Executar

Na raiz do repositório, com o SDK .NET 10 instalado no Windows:

```powershell
dotnet run --project CursorHub/CursorHub.csproj
```

Para as verificações de lógica:

```powershell
dotnet run --project CursorHub/Checks/Checks.csproj -c Release
```

## Estrutura

| Caminho | Responsabilidade |
| :--- | :--- |
| `CursorHubGame.cs` | Ciclo da aplicação, demonstração e integração com os efeitos globais. |
| `Interface/` | Layout, controles, temas, presets e persistência dos ajustes. |
| `Core/` | Trajetória do rastro, estados de reprodução, cores e escala da interface. |
| `Windows/` | Cursor nativo, sobreposição, histórico do mouse, seletores e imagens. |
| `Assets/` | Recursos da interface e atlas da fonte Kode Mono. |
| `Checks/` | Verificações de lógica e matriz de estados/trajetórias. |
| `Verification.cs`, `PresetVerification.cs` | Integração e interação automatizada no Windows. |
| `Tools/` | Fontes gráficas e ferramentas de geração de recursos. |

## Presets e salvamento

Cada preset guarda cursor e rastro com tamanhos, imagens e cores independentes, além de hotspot, duração, limite de desenho e economia. A galeria mostra os presets existentes e um único card **NOVO**.

O menu de três pontos abre as ações de usar, editar e excluir. O editor tem abas **CURSOR**, **RASTRO** e **AJUSTES**. Tanto **SALVAR PRESET** no editor quanto o botão lateral salvam o rascunho aberto. Sem seleção, o botão lateral abre a criação de um novo preset.

A miniatura do preset selecionado usa os ajustes atuais ao voltar à galeria. Para persistir alterações feitas nas abas principais, use **SALVAR PRESET**. Cancelar o editor descarta o rascunho. Erros de gravação aparecem no editor e permitem tentar novamente.

Imagens PNG/SVG preservam suas cores; os seletores de cor aparecem apenas no formato circular. O editor de imagem permite rotação em passos de 15 graus, inversão e definição do hotspot por clique ou arraste.

## Dados locais

Os dados ficam em `%LOCALAPPDATA%\CursorHub`: presets, configurações, preferências, imagens importadas e cache de recursos. Mover apenas o executável não remove esses dados. Para migrar a coleção, copie também a pasta de dados.

## Efeitos e desempenho

**PLAY** ativa os efeitos; **STOP**, **Ctrl + Shift + X** ou fechar o aplicativo solicita a interrupção e restauração do cursor. Mudanças em um efeito já ativo devem ser aplicadas com STOP e PLAY.

A captura pode recuperar pontos do histórico real do Windows. O rastro usa buffers limitados e uma camada nativa com alfa por pixel, dimensionada conforme a trajetória. Há orçamento de 256/512/1024 partículas, limite de desenho entre 30 e 360 Hz e economia da demonstração parada a 15 Hz.

Maximizar preserva a proporção da interface e o tamanho físico do cursor. O tema é aplicado imediatamente. Compatibilidade com cursores próprios de outros aplicativos, tela cheia exclusiva e áreas seguras do Windows não é garantida.

## Publicação

O workflow [Windows portable release](../.github/workflows/release.yml) incorpora os recursos e publica para `win-x64` com runtime e bibliotecas nativas incluídos. Apenas o executável é copiado para a pasta de teste antes da validação e do empacotamento.

O workflow é acionado por mudanças em `CursorHub/RELEASE_VERSION`, mudanças no próprio workflow ou execução manual. O artefato final contém o `.exe` e `SHA256SUMS.txt`. A promoção de uma versão validada para release oficial é uma etapa editorial no GitHub.

A execução pelo código-fonte usa `Assets/`; a distribuição incorpora os recursos em `AssetBundle.zip` e prepara o cache automaticamente. A fonte Kode Mono acompanha sua licença OFL. O atlas pode ser regenerado com `Tools/Build-Font.py` e Pillow 12.1.0.

## Validação da v0.10.1

[Execução aprovada e relatórios](https://github.com/WKMcode-dev/Cursor_HUB/actions/runs/39869850187).

- Lógica: 199 cenários de estados e trajetórias, migração, RGB e limites de paginação.
- Interface: cliques e digitação reais, sliders, presets, miniaturas, temas e escala.
- Integração: diálogos RGB, seleção de SVG no gerenciador, rotação, inversão e hotspot.
- Resiliência: nome inválido, imagem corrompida, importação atrasada, erro de gravação e nova tentativa.
- Persistência: reabertura em outro processo preservando preset, seleção, cores, imagens e hotspot.
- Desktop: composição sobre janelas independentes, tamanhos separados e ciclos rápidos de Play/Stop.

Os artifacts de verificação incluem capturas e relatórios JSON. Esses testes não substituem validação em todos os monitores, drivers e jogos.
