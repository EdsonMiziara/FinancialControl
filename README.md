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

## 🚀 Como Funciona
O código percorre o diretório configurado, busca por arquivos de extrato, evita duplicidades e alimenta a aba `CONTROLE`. A partir daí, a planilha utiliza fórmulas como `SOMASES` para gerar um resumo financeiro em tempo real na aba `RESUMO`.

---
Desenvolvido por Edson Miziara
