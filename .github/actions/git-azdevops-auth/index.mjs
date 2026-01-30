import console from "node:console";
import { spawn } from "node:child_process";
import assert from "node:assert";
import fs from "node:fs/promises";
import path from "node:path";
import { randomUUID } from "node:crypto";

import.meta.resolve("@actions/core").catch(() => {
  const npmArgs = [
    "clean-install",
    "--omit=dev",
    "--omit=peer",
    "--omit=optional",
    "--fund=false",
    "--audit=true",
    "--install-links=true",
  ];

  const npmCommand = `npm ${npmArgs.join(" ")}`;
  console.log(`[command]${npmCommand}`);
  const npmSpawn = spawn(npmCommand, {
    shell: true,
    stdio: "inherit",
    cwd: import.meta.dirname,
  });
  npmSpawn.on("exit", (exitCode) => {
    console.log(`npm exited with exit code: ${exitCode}`);
    if (exitCode === 0) console.log("Setup complete");
  });
});

import * as core from "@actions/core";
import { exec } from "@actions/exec";

import {
  AzureCloudInstance,
  ConfidentialClientApplication,
  LogLevel,
} from "@azure/msal-node";

const msalAssertionAudience =
  core.getInput("audience") || "api://AzureADTokenExchange";

/** @type {import("@azure/msal-node").Configuration} */
const msalConfig = {
  auth: {
    clientId: core.getInput("client-id", { required: true }),
    /** @type {import("@azure/msal-node").ClientAssertionCallback} */
    clientAssertion: core.getIDToken.bind(undefined, msalAssertionAudience),
    azureCloudOptions: {
      azureCloudInstance:
        core.getInput("instance") || AzureCloudInstance.AzurePublic,
      tenant: core.getInput("tenant", { required: true }),
    },
  },
  system: {
    loggerOptions: {
      loggerCallback(level, message) {
        if (level <= LogLevel.Error) {
          core.error(message);
        } else if (level <= LogLevel.Warning) {
          core.warning(message);
        } else if (level <= LogLevel.Info) {
          core.notice(message);
        } else if (level <= LogLevel.Verbose) {
          core.debug(message);
        } else if (core.isDebug()) {
          core.debug(message);
        }
      },
      logLevel: core.isDebug() ? LogLevel.Trace : LogLevel.Verbose,
      piiLoggingEnabled: false,
    },
  },
};

const msalClient = new ConfidentialClientApplication(msalConfig);
/** @type {import("@azure/msal-node").ClientCredentialRequest} */
const msalRequest = {
  scopes: ["https://dev.azure.com"],
};
const msalAuthResult =
  await msalClient.acquireTokenByClientCredential(msalRequest);
core.setSecret(msalAuthResult.accessToken);

const runnerTempPath = process.env["RUNNER_TEMP"] || "";
assert.ok(runnerTempPath, "RUNNER_TEMP is not defined");

const runnerTempFiles = await fs.readdir(runnerTempPath);
const gitCredentialsFile = runnerTempFiles.find(
  /git-credentials-[0-9a-f-]+\.config$/i.test,
);
assert.ok(gitCredentialsFile, "git-credentials configuration file not found");
const gitCredentialsPath = path.join(runnerTempPath, gitCredentialsFile);

const tokenPlaceholderConfigValue = `Authorization: Bearer placeholder-${randomUUID()}`;
const tokenConfigValue = `Authorization: Bearer ${msalAuthResult.accessToken}`;
const gitConfigExitCode = await exec("git", [
  "config",
  "set",
  "--file",
  gitCredentialsPath,
  `http.${"https://dev.azure.com/"}.extraheader`,
  tokenPlaceholderConfigValue,
]);
assert.strictEqual(
  gitConfigExitCode,
  0,
  "Git credentials configuration failed",
);
let gitCredentialsText = await fs.readFile(gitCredentialsPath, {
  encoding: "utf-8",
});
gitCredentialsText = gitCredentialsText.replaceAll(
  tokenPlaceholderConfigValue,
  tokenConfigValue,
);
await fs.writeFile(gitCredentialsPath, gitCredentialsText, {
  encoding: "utf-8",
});
