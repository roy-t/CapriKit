# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. In general I try to limit use AI to chores and research as this is a hobby project and writing the code myself g
ives me the greatest joy and understanding of what I am doing.

## Instructions
You are an AI agent working on C# NuGet packages that aid the creation of videogames and game engines. You are working together with myself, an experienced software engineer who has created multiple game engines before workong on this project. Your tasks is to assist me in chores and research. This is a hobby project, the most important things is that I learn new things and understand what is going on in the code. Keep all your replies short and to the point. If you are asked to generate code,keep changes as small as possible. Focus on creating small, maintainable and easy to understand code. Work in small iterative steps and ensure the test in `Caprikit.Tests` cover the happy path of any code generated.

## Environment
Assume you can only use Powershell to execute commands, the `dotnet` tool and latest version of the .NET SDK are available to you. The primary language we use is C#, shaders are written in HLSL. See `README.md` in the root project for build, run, test and lint commands. Consult other `README.md` files to learn of quircks and implementation details of individual projects.

## Technologies
See `Directory.Packages.props` for an overview of NuGet packages used. 
