#pragma once

// QT Include(s)
#include <QApplication>
#include <QLocalServer>
#include <QLocalSocket>

class SingletonApp  : public QApplication {
	Q_OBJECT
public:
	              SingletonApp(int argc, char** argv);
	void          setApplicationName(const QString& appName);
	inline bool   isAlreadyRunning() const { return m_isRunning; }
Q_SIGNALS:		  
	void          newInstanceDetected();
private:
	bool          checkIfRunning();
	void          handleNewConnection();
	void          listenForNewInstance();
	QString       m_appName;
	QLocalServer* m_localServer;
	bool          m_isRunning;
};