// Project Include(s)
#include "SingletonApp.h"

SingletonApp::SingletonApp(int argc, char** argv): QApplication(argc, argv),
	m_isRunning(false), m_localServer(Q_NULLPTR) {};

void SingletonApp::setApplicationName(const QString& appName) {
	m_appName = appName;
	m_isRunning = checkIfRunning();
	if (!m_isRunning) {
		listenForNewInstance();
	}
}

void SingletonApp::listenForNewInstance() {
	if (!m_isRunning) {
		m_localServer = new QLocalServer(this);
		connect(m_localServer, &QLocalServer::newConnection, this, &SingletonApp::handleNewConnection);
		if (!m_localServer->listen(m_appName)) {
			qDebug() << "[SingletonApp::listenForNewInstance]Failed to listen on local server:" << m_localServer->errorString();
		}
	}
}

bool SingletonApp::checkIfRunning() {
	QLocalSocket socket;
	socket.connectToServer(m_appName);
	if (socket.waitForConnected(100)) {
		// 说明有相同实例在运行了
		socket.write("show");
		socket.flush();
		socket.waitForBytesWritten(100);
		return true;
	}
	return false;
}

void SingletonApp::handleNewConnection() {
	QLocalSocket* socket = m_localServer->nextPendingConnection();
	if (socket) {
		connect(socket, &QLocalSocket::readyRead, [this, socket]() {
			QByteArray data = socket->readAll();
			if (data == "show") {
				emit newInstanceDetected();
			}
			socket->disconnectFromServer();
		});
	}
}
