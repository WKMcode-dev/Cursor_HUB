# Cursor Hub 0.7.0

Windows / C# / .NET 10 / osu!framework.

## Versao pronta para Windows

Baixe o ZIP win-x64 na [release v0.7.0](https://github.com/WKMcode-dev/Cursor_HUB/releases/tag/v0.7.0), extraia toda a pasta e abra CursorHub.exe. O runtime do .NET esta incluido; Visual Studio e SDK nao sao necessarios. Mantenha DLLs e Assets junto do executavel.

## Executar o codigo-fonte

Extraia para uma pasta nova e abra INICIAR.cmd, ou execute:

```powershell
dotnet run --project CursorHub.csproj
```

## Janela

Os controles do recorte superior são bolinhas no estilo macOS: vermelha fecha, amarela minimiza e verde maximiza/restaura. Os símbolos aparecem ao passar o mouse. A região nativa acompanha os círculos, sem fundos retangulares. Play/Stop fica no recorte inferior direito. A região nativa inclui apenas a moldura e as ilhas dos controles; os vazios continuam fora da janela. O recorte usa dimensões nativas válidas e é recalculado ao retornar da minimização. Maximizar utiliza a área útil do monitor.

## Presets

Na aba Presets, use + Adicionar preset. Escolha nome, tamanho, duração e cor; Salvar grava o preset localmente. Cada cartão tem um menu de três pontos com Demonstrar, Editar e Excluir. Clicar no nome também demonstra o preset. A coleção tem paginação e aceita até 128 presets. Os arquivos ficam em %LOCALAPPDATA%\CursorHub.

Demonstrar carrega o preset na prévia. Não altera o cursor global que já esteja em execução: pare e pressione Play novamente para aplicar as novas escolhas.

## Play global

Play liga o cursor circular personalizado e seu rastro no desktop, sem temporizador. Funciona independentemente da janela do app estar na frente ou minimizada. Stop ou Ctrl+Shift+X desliga. Fechar o app também solicita a restauração dos cursores originais.

A interface e a prévia continuam em osu!framework. Para evitar a tela preta do antigo teste, o desktop agora usa uma janela Win32 separada com transparência alfa por pixel (UpdateLayeredWindow), sem ampliar ou transformar a janela principal. Ela não recebe cliques nem foco. O núcleo de amostragem do rastro é compartilhado com a prévia.

O cursor nativo é trocado com SetSystemCursor. Antes de modificar qualquer papel do cursor, um processo auxiliar guarda cópias dos originais. Ele espera o comando Stop ou o fechamento do canal do processo principal para restaurar. Não há alteração de arquivos do tema nem pedido de privilégios elevados. Se o Windows impedir a troca, o rastro continua ativo com o cursor original e a interface informa essa condicao. Aplicativos que desenham seu próprio cursor, tela cheia exclusiva e a área segura do Windows não têm compatibilidade garantida.

## Desempenho

256 amostras em buffer fixo. Os objetos da prévia são reutilizados. O desktop usa um DIB persistente de 1024x1024 e envia somente o retângulo ativo do rastro; pontos a mais de 450 pixels por eixo da ponta são descartados no desenho, limitando o custo de movimentos muito rápidos. Depois de expirar o rastro, a camada deixa de ser redesenhada até haver movimento. O cursor principal é nativo e continua acompanhando o mouse sem depender de desenhar a interface.

Os limites de 60/120 Hz são metas, sujeitos ao escalonamento do Windows. A opção de economia reduz a interface parada a 15 Hz; não reduz a consulta do mouse pelo worker global. CPU/GPU, latência e múltiplas escalas DPI ainda precisam de medições controladas. Não há promessa de impacto zero.

## Validação

Compilação completa e testes de preenchimento, expiração, limite de amostras, alfa transparente/pre-multiplicado e persistência/edição/exclusão de presets. A verificação em execução também gera capturas do próprio backbuffer para conferir presets, menu, editor, restauração e maximização. Consulte o relato da entrega para o resultado exato do teste global.

Neste ambiente, SetSystemCursor retornou acesso negado. Portanto, a substituição efetiva e a restauração global do cursor precisam ser confirmadas em uma execução normal no Windows. A versao 0.3.3 mantem o rastro ativo nessa situacao. Isso é separado da transparência da camada nativa.

Os avisos de segurança do ImageSharp 3.1.11 continuam pendentes, sem supressão no projeto. A migração para a versão principal corrigida requer validar a compatibilidade com osu!framework.

## Código

Interface: HubLayout.cs, PresetLayout.cs, HubButton.cs. Persistência: HubSettings.cs e PresetStore.cs. Integração: WindowChrome.cs, DesktopEffects.cs, LayeredSurface.cs e CursorController.cs. Core/TrailBuffer.cs permanece independente da interface.

```powershell
dotnet run --project Checks/Checks.csproj
```

O modo técnico --verify PASTA executa um ciclo breve de verificação, grava capturas internas e encerra. Ele não salva presets de exemplo. Não é necessário para usar o app.


## Correcao 0.3.2: contornos pretos

Os controles macOS e o Play usam PNGs RGBA, gerados em 4x e carregados uma vez. A regiao nativa agora e construida a partir do alfa das mesmas imagens e da moldura; nao ha uma segunda aproximacao geometrica das bolinhas. A borda adicional do DWM tambem e desativada quando suportado pelo Windows.

Na 0.3.2 o recorte binario eliminava o halo, mas cortava a suavizacao. Na 0.3.3 os pixels parciais sao recuperados por uma camada alfa complementar; o compositor do rastro tambem continua com alfa por pixel.

O recorte e recalculado apenas ao mudar o tamanho ou restaurar a janela. O modo --verify-ui PASTA verifica a interface sem ativar a troca global do cursor. Ele compara a regiao calculada com os pixels renderizados para detectar fundo preto visivel.


## Correcao 0.3.3: suavizacao e rastro independente

As bordas semitransparentes dos PNGs sao desenhadas por uma janela alfa pertencente a janela principal. O interior dessa camada e totalmente transparente, e ela nao intercepta cliques. A interface principal continua no osu!framework. Essa camada usa as cores pre-multiplicadas dos PNGs e apenas complementa os pixels que o recorte nativo nao consegue representar, preservando a suavizacao sobre fundos claros e escuros sem copiar o backbuffer da GPU.

A textura de bordas so e reconstruida quando o tamanho muda ou a janela e restaurada. Mover a janela reposiciona a camada existente, sem recalcular a imagem. Minimizar oculta a camada. A camada nao e globalmente topmost e acompanha a janela proprietaria.

A troca do cursor do sistema e a apresentacao do rastro foram desacopladas. Falhas de SetSystemCursor nao cancelam o rastro. O worker global permanece ativo ao minimizar a interface. STOP ou Ctrl+Shift+X continua desligando o efeito e solicitando a restauracao dos cursores.

Verificacao desta entrega: compilacao sem erros; capturas internas sem pixels pretos na regiao opaca; 3077 pixels de alfa parcial presentes na borda no tamanho testado; compositor global apresentou 12 quadros e permaneceu ativo ao minimizar mesmo com SetSystemCursor negado. Nao houve teste direto dentro de um jogo. Tela cheia exclusiva, cursores proprios dos jogos e politicas de sobreposicao podem limitar a exibicao; use janela sem bordas para testar.


## Correcao 0.7.0: visibilidade acima de outras janelas

A camada global nasce com WS_EX_TOPMOST e recebe ShowWindow(SW_SHOWNOACTIVATE) antes da reafirmacao da ordem de janelas. SetWindowPos agora tem retorno verificado, assim como IsWindowVisible e a flag topmost. Uma falha passa a aparecer como erro em vez de ser confundida com apresentacao bem-sucedida. O worker usa coordenadas fisicas por monitor para evitar deslocamentos em escalas DPI diferentes.

O teste --verify-overlay cria duas janelas proprias independentes e verifica a cor da camada na composicao final do desktop. Tambem exercita o worker real do rastro com posicao controlada e procura seus pixels acima da janela de verificacao. Isso e diferente de apenas contar quadros enviados. Nao le conteudo de navegadores ou jogos.

DIAGNOSTICO.cmd executa esse teste e grava diagnostico-overlay/overlay-results.json. Se ainda houver problema, esse arquivo permite distinguir falha de visibilidade, posicionamento ou apresentacao.

Feche a versao anterior antes de iniciar a nova. A partir desta versao uma segunda instancia exibe um aviso, em vez de encerrar silenciosamente. Jogos em tela cheia exclusiva continuam sem suporte garantido; use janela sem bordas. Nao ha tentativa de contornar restricoes de sobreposicao de jogos.

## Visual Character Chain / 0.7.0

Paleta perolada com texto grafite, ciano, rosa e lavanda. Grade de 24 slots por pagina, miniaturas compartilhadas, indicacao do preset demonstrado e menu lateral de acoes. As posicoes livres nao representam presets salvos. Os arquivos pessoais existentes sao preservados. A previa compacta no topo da colecao responde ao mouse; Cursor, Rastro e Desempenho ampliam a previa.

Os vetores originais em Tools/Art geram PNGs de alta resolucao uma unica vez. O app carrega e compartilha cada textura por nome. Nao ha shaders animados, blur ou geracao de imagens em tempo de execucao. A pre-visualizacao atualiza apenas os pontos ativos, e o rasterizador do desktop calcula raiz quadrada somente na borda suavizada das particulas. A comparacao automatizada verifica os pixels com a formula anterior; isso nao substitui medicoes de CPU/GPU e latencia em hardware real.

## 0.7.0: SVGs originais e escala proporcional

Cards quadrados e barra vertical de acoes usam os SVGs fornecidos pelo autor. A barra oferece Excluir, Editar e Demonstrar (de cima para baixo). As bolinhas da janela derivam do SVG Preset_use. O layout agora tem 1100x500 unidades, com demonstracao permanente a direita. Ao maximizar, uma unica escala preserva as proporcoes; espacos excedentes ficam transparentes, fora da regiao de cliques. O tamanho do cursor da previa e medido em pixels fisicos, independente da escala da interface.

A verificacao automatizada confere 8 px antes e depois de maximizar e restaurar, alem do recorte e do compositor global. Os vetores fonte ficam em Tools/Art e sao rasterizados somente no desenvolvimento.

## 0.7.0

Limite manual de 30 a 360Hz (atalhos 60,75,144,165), aplicado ao rastro ativo sem trocar novamente o cursor. A frequencia real depende da capacidade do sistema. A taxa nao altera as configuracoes do monitor. A economia da previa parada continua separada do compositor.

MouseHistory usa GetMouseMovePointsEx (Microsoft Learn: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmousemovepointsex) para recuperar posicoes intermediarias reais. Historico de ate64pontos, ordem cronologica, descarte de tempos antigos e tratamento de coordenadas negativas. Falha na API recai na captura atual; nao se aplica spline que invente o trajeto. O buffer de512pontos limita memoria e trabalho, podendo descartar o inicio de um rastro longo em movimentos muito rapidos.
