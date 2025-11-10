"""Networking utilities to send pose payloads."""

from __future__ import annotations

import socket
from typing import Optional


class PoseSender:
    """Maintains a TCP socket connection and sends serialized pose strings."""

    def __init__(self, host: str = "127.0.0.1", port: int = 25001, timeout: float = 2.0) -> None:
        self._host = host
        self._port = port
        self._timeout = timeout
        self._socket: Optional[socket.socket] = None

    def send(self, payload: str) -> None:
        if not payload:
            return

        sock = self._ensure_socket()
        if sock is None:
            return

        try:
            sock.sendall(payload.encode("utf-8"))
        except OSError:
            self._reset_socket()
            sock = self._ensure_socket()
            if sock is not None:
                try:
                    sock.sendall(payload.encode("utf-8"))
                except OSError:
                    self._reset_socket()

    def _ensure_socket(self) -> Optional[socket.socket]:
        if self._socket is None:
            try:
                new_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                new_socket.settimeout(self._timeout)
                new_socket.connect((self._host, self._port))
                self._socket = new_socket
            except OSError:
                self._reset_socket()
        return self._socket

    def _reset_socket(self) -> None:
        if self._socket is not None:
            try:
                self._socket.close()
            except OSError:
                pass
        self._socket = None

    def close(self) -> None:
        self._reset_socket()

    def __del__(self) -> None:
        self.close()

