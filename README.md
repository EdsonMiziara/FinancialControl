# 💰 FinancialControl - Automação de Fluxo de Caixa

Este projeto nasceu de uma necessidade real: automatizar a gestão financeira de um Microempreendedor Individual (MEI). O sistema processa arquivos de extrato bancário (OFX) e consolida os dados de forma inteligente em uma planilha Excel estruturada para análise.

## 🔴 O Problema
A gestão manual de entradas e saídas a partir de extratos bancários é lenta, sujeita a erros humanos e cansativa. Para quem utiliza o banco Sicoob ou similares, extrair esses dados e categorizá-los manualmente no Excel demanda um tempo que poderia ser usado no core business.

## 🟢 A Solução
O **FinancialControl** é uma aplicação Console em C# que:
1. Lê múltiplos arquivos `.ofx` simultaneamente.
2. Normaliza os dados (Data, Descrição, Valor).
3. Identifica automaticamente o tipo de transação (Receita/Despesa).
4. Exporta tudo para uma planilha Excel (`Controle_Financeiro.xlsx`).
5. Mantém a formatação condicional (Verde para Receitas, Vermelho para Despesas) e atualiza tabelas de resumo automaticamente.

## 🛠️ Tecnologias Utilizadas
* **C# (.NET 8.0/9.0):** Linguagem robusta para processamento de dados.
* **ClosedXML:** Biblioteca para manipulação avançada de arquivos Excel sem a necessidade de ter o Office instalado.
* **LINQ:** Utilizado para filtragem e organização eficiente das transações.
* **OFX Parser:** Lógica customizada para leitura de arquivos de intercâmbio financeiro.

## ⚙️ Como Configurar e Executar

### Pré-requisitos
* [.NET SDK](https://dotnet.microsoft.com/download) (Versão 8.0 ou superior)
* Um editor de código como **VS Code** ou **Visual Studio**
* Arquivos `.ofx` (exportados do seu Internet Banking)

### 🚀 Instalação
1. Clone o repositório:
   ```bash
   git clone [https://github.com/EdsonMiziara/FinancialControl.git](https://github.com/EdsonMiziara/FinancialControl.git)
   ```

## 🚀 Como Funciona
O código percorre o diretório configurado, busca por arquivos de extrato, evita duplicidades e alimenta a aba `CONTROLE`. A partir daí, a planilha utiliza fórmulas como `SOMASES` para gerar um resumo financeiro em tempo real na aba `RESUMO`.

2. Navegue até a pasta do projeto:

```bash
cd FinancialControl
```
3. Restaure as dependências (ClosedXML):

```bash
dotnet restore
```
## 💻 Como Usar
4. Coloque seus arquivos .ofx na pasta raiz do projeto ou na pasta de inputs configurada no código.

Certifique-se de que a planilha Controle_Financeiro.xlsx não está aberta.

Execute a aplicação:

```bash
dotnet run
```
O console avisará quando a planilha for atualizada com sucesso.

## 📈 Estrutura da Planilha
Para que o sistema funcione perfeitamente, a planilha gerada segue este padrão:

Aba CONTROLE: Onde os dados brutos são inseridos.

Aba RESUMO: Onde as fórmulas de SOMASES consolidam os valores por categoria (ex: Extra, Alimentação, Serviços) e tipo (Receita/Despesa).

---
Desenvolvido por Edson Miziara
